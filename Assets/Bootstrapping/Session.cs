using System.Collections;
using System.Text;
using Shooter.Accounts;
using Shooter.Client.Interface;
using Shooter.Configuring;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Bootstrapping
{
    internal class Session : MonoBehaviour
    {
        private const string NetworkPrefab = "NetworkManager";
        private const string OverlayPrefab = "Overlays";
        private const string CompressionPrefab = "Compression";
        private const string VhsPrefab = "VHS";
        private const string ScreenshotPrefab = "Screenshots";
        private const string MenuScene = "Menu";
        private const string BootScene = "Boot";
        private const string WorldScene = "Map";
        private const string AnyAddress = "0.0.0.0";
        private const int MenuFrameRate = 60;
        private const int UnlimitedFrameRate = -1;
        private static readonly Journal Log = Logs.Here();
        private bool ending;
        private bool loadFailed;
        private LoadingOverlay loading;

        private GameObject overlays;

        private IEnumerator Start()
        {
            Raise(CompressionPrefab);
            Raise(VhsPrefab);
            Raise(ScreenshotPrefab);
            yield return ToMenu();
        }

        private IEnumerator ToMenu()
        {
            Drop();
            Application.targetFrameRate = MenuFrameRate;

            yield return SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);

            MenuScreen screen = FindAnyObjectByType<MenuScreen>();
            if (screen == null)
            {
                Log.Error($"Scene {MenuScene} carries no menu screen, there is nothing to press");
                yield break;
            }

            screen.Hosting += Host;
            screen.Joining += Join;
            screen.Quitting += Quit;

            if (loadFailed)
            {
                loadFailed = false;
                screen.Warn("Не удалось загрузить сохранение", "Файл повреждён или несовместим с этой версией");
            }

            ending = false;
            Log.Info("Menu is up");
        }

        private void Host(string save)
        {
            StartCoroutine(Begin(true, save));
        }

        private void Join()
        {
            StartCoroutine(Begin(false, null));
        }

        private void Quit()
        {
            Log.Info("Leaving the game by the player's own hand");
            Application.Quit();
        }

        private IEnumerator Begin(bool hosting, string save)
        {
            Application.targetFrameRate = UnlimitedFrameRate;
            yield return SceneManager.LoadSceneAsync(BootScene, LoadSceneMode.Single);

            Overlays();

            if (hosting)
            {
                loading.Show(LoadingStage.Scene);
                yield return SceneManager.LoadSceneAsync(WorldScene, LoadSceneMode.Single);
            }

            NetworkManager network = Network();
            if (network == null)
            {
                yield return ToMenu();
                yield break;
            }

            ClientConfig client = Config.Read().Client;

            Address(network, hosting);
            network.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(client.Name);
            network.OnServerStopped += Stopped;
            network.OnClientStopped += Stopped;

            loading.Show(hosting ? LoadingStage.Server : LoadingStage.Connection);

            if (!(hosting ? network.StartHost() : network.StartClient()))
            {
                Log.Error($"The {(hosting ? "host" : "client")} refused to start");
                yield return ToMenu();
                yield break;
            }

            Log.Info($"{(hosting ? "Host" : "Client")} is up as {client.Name}");

            if (!hosting)
            {
                network.SceneManager.OnSceneEvent += Synchronizing;
                network.OnClientConnectedCallback += Entered;
                yield break;
            }

            if (save != null) Restore(network, save);
            if (network.ShutdownInProgress) yield break;

            loading.Hide();
        }

        private void Restore(NetworkManager network, string save)
        {
            SaveManager saves = SaveManager.Current;
            if (saves == null)
            {
                Log.Error($"World has no save manager, {save} stays unloaded, shutting the host down");
                loadFailed = true;
                network.Shutdown();
                return;
            }

            Log.Info($"Loading the world from {save}");
            loading.Show(LoadingStage.Save);
            FrozenWorld world = saves.Freeze();
            if (saves.Load(world, save)) return;

            Log.Warn($"The world failed to load {save}, shutting the host down");
            loadFailed = true;
            network.Shutdown();
        }

        private void Synchronizing(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:
                    loading.Show(LoadingStage.Scene);
                    break;
                case SceneEventType.LoadComplete:
                    loading.Show(LoadingStage.Synchronization);
                    break;
            }
        }

        private void Entered(ulong ignored)
        {
            loading.Hide();
        }

        private void Stopped(bool ignored)
        {
            if (ending) return;

            ending = true;
            StartCoroutine(End());
        }

        private IEnumerator End()
        {
            Log.Info("The session is over, heading back to the menu");

            yield return null;
            yield return ToMenu();
        }

        private void Drop()
        {
            if (overlays != null)
            {
                Destroy(overlays);
                overlays = null;
                loading = null;
            }

            if (NetworkManager.Singleton != null) Destroy(NetworkManager.Singleton.gameObject);
        }

        private void Address(NetworkManager network, bool hosting)
        {
            UnityTransport transport = network.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Log.Warn("No unity transport to configure");
                return;
            }

            transport.UseEncryption = true;

            if (hosting)
            {
                ServerConfig server = Config.Read().Server;
                transport.SetConnectionData(AnyAddress, server.Port, AnyAddress);

                Account account = Config.Account;
                transport.SetServerSecrets(account.Certificate, account.PrivateKeyPem);
                Log.Info($"World listens on port {server.Port}, channel encrypted");
                return;
            }

            ClientConfig client = Config.Read().Client;
            if (!Invite.TryDecode(client.Invite, out string address, out ushort port, out string certificate))
            {
                Log.Error("Invite code is missing or malformed, staying in the menu");
                return;
            }

            transport.SetConnectionData(address, port);
            transport.SetClientSecrets(Account.CommonName, certificate);
            Log.Info($"Heading for {address}:{port}, channel encrypted");
        }

        private void Raise(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                Log.Error($"No {prefabName} prefab in Resources, that service stays down");
                return;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = prefabName;
            DontDestroyOnLoad(instance);
            Log.Info($"{prefabName} is up");
        }

        private void Overlays()
        {
            GameObject prefab = Resources.Load<GameObject>(OverlayPrefab);

            overlays = Instantiate(prefab);
            overlays.name = OverlayPrefab;
            DontDestroyOnLoad(overlays);
            loading = overlays.GetComponentInChildren<LoadingOverlay>();
            Log.Info("Overlays are up");
        }

        private NetworkManager Network()
        {
            if (NetworkManager.Singleton != null) return NetworkManager.Singleton;

            GameObject prefab = Resources.Load<GameObject>(NetworkPrefab);
            if (prefab == null)
            {
                Log.Error($"No {NetworkPrefab} prefab in Resources, refusing to start");
                return null;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = NetworkPrefab;
            DontDestroyOnLoad(instance);

            NetworkManager network = instance.GetComponent<NetworkManager>();
            if (network == null) Log.Error($"Prefab {NetworkPrefab} has no NetworkManager component");

            return network;
        }

    }
}
