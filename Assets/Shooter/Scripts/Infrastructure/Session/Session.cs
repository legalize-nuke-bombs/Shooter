using System.Collections;
using System.Text;
using Shooter.Game;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    internal class Session : MonoBehaviour
    {
        private const string NetworkPrefab = "NetworkManager";
        private const string OverlayPrefab = "Overlays";
        private const string StatePrefab = "GameState";
        private const string MenuScene = "Menu";
        private const string BootScene = "Boot";
        private const string WorldScene = "Map";
        private const string AnyAddress = "0.0.0.0";
        private const int MenuFrameRate = 60;
        private const int UnlimitedFrameRate = -1;
        private const string BrokenSave = "Не удалось загрузить сохранение";
        private const string BrokenSaveDetails = "Файл повреждён или несовместим с этой версией";
        private static readonly Journal Log = Logs.Here();
        private bool ending;
        private bool worldLoaded;
        private bool freezing;
        private FrozenWorld frozen;
        private LoadingOverlay loading;
        private MenuScreen menu;
        private string warning;
        private string warningDetails;

        private GameObject overlays;

        private IEnumerator Start()
        {
            Mixer.Tune();
            yield return ToMenu();
        }

        private IEnumerator ToMenu()
        {
            Drop();
            Application.targetFrameRate = MenuFrameRate;

            yield return SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);

            menu = FindAnyObjectByType<MenuScreen>();
            if (menu == null)
            {
                Log.Error($"Scene {MenuScene} carries no menu screen, there is nothing to press");
                yield break;
            }

            menu.Hosting += Host;
            menu.Joining += Join;
            menu.Quitting += Quit;

            if (warning != null)
            {
                menu.Warn(warning, warningDetails);
                warning = null;
                warningDetails = null;
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

        // What can fail before the network starts is checked while the menu is still up, so the player stays in it
        private IEnumerator Begin(bool hosting, string save)
        {
            NetworkManager network = Network();
            if (network == null) yield break;

            string certificate = Address(network, hosting);
            if (certificate == null)
            {
                Drop();
                yield break;
            }

            Application.targetFrameRate = UnlimitedFrameRate;
            yield return SceneManager.LoadSceneAsync(BootScene, LoadSceneMode.Single);

            Overlays();

            ClientConfig client = Config.Read().Client;
            network.NetworkConfig.ConnectionData = Handshake.Encode(client.Name, Config.Account, certificate);
            network.OnServerStopped += Stopped;
            network.OnClientStopped += Stopped;

            if (hosting) loading.Show(LoadingStage.Server);
            else loading.Show(LoadingStage.Connection, Cancel);

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
                network.OnClientDisconnectCallback += Refused;
                yield break;
            }

            loading.Show(LoadingStage.Scene);
            if (!State())
            {
                network.Shutdown();
                yield break;
            }

            yield return LoadWorld(network, save != null);
            if (network.ShutdownInProgress) yield break;

            if (save != null) Restore(network, save);
            if (network.ShutdownInProgress) yield break;

            network.GetComponent<Greeter>().Ready();
            loading.Hide();
        }

        private IEnumerator LoadWorld(NetworkManager network, bool freeze)
        {
            worldLoaded = false;
            freezing = freeze;
            frozen = null;
            SaveManager.Open();
            network.SceneManager.OnLoadComplete += Loaded;

            SceneEventProgressStatus status = network.SceneManager.LoadScene(WorldScene, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                network.SceneManager.OnLoadComplete -= Loaded;
                Log.Error($"The world refused to load: {status}, shutting the host down");
                network.Shutdown();
                yield break;
            }

            yield return new WaitUntil(() => worldLoaded || network.ShutdownInProgress);
            network.SceneManager.OnLoadComplete -= Loaded;
            Log.Info($"World {WorldScene} is up");
        }

        private void Loaded(ulong clientId, string sceneName, LoadSceneMode mode)
        {
            if (clientId != NetworkManager.ServerClientId || sceneName != WorldScene) return;

            worldLoaded = true;

            // Netcode calls this before the scene's first Update, the coroutine waiting on worldLoaded resumes after it:
            // freezing here keeps that one Update from running on a world the save has not loaded yet
            if (freezing) frozen = SaveManager.Freeze();
        }

        private void Restore(NetworkManager network, string save)
        {
            if (frozen == null)
            {
                Log.Error($"World was not frozen on load, {save} stays unloaded, shutting the host down");
                WarnInMenu(BrokenSave, BrokenSaveDetails);
                network.Shutdown();
                return;
            }

            Log.Info($"Loading the world from {save}");
            loading.Show(LoadingStage.Save);
            FrozenWorld world = frozen;
            frozen = null;
            if (SaveManager.Load(world, save)) return;

            Log.Warn($"The world failed to load {save}, shutting the host down");
            WarnInMenu(BrokenSave, BrokenSaveDetails);
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
            NetworkManager.Singleton.OnClientDisconnectCallback -= Refused;
            loading.Hide();
        }

        private void Cancel()
        {
            NetworkManager network = NetworkManager.Singleton;
            network.OnClientDisconnectCallback -= Refused;
            network.Shutdown();
        }

        // The client dropped before it got into the world. Read here and not on the stop:
        // the shutdown that follows overwrites the transport's verdict
        private void Refused(ulong ignored)
        {
            NetworkManager network = NetworkManager.Singleton;
            NetworkTransport.DisconnectEvents verdict = network.NetworkConfig.NetworkTransport.DisconnectEvent;
            Log.Warn($"The client did not get into the world: {verdict}, {network.DisconnectReason}");

            switch (verdict)
            {
                case NetworkTransport.DisconnectEvents.ClosedByRemote:
                    WarnInMenu("Сервер отклонил подключение", network.DisconnectReason);
                    break;
                case NetworkTransport.DisconnectEvents.AuthenticationFailure:
                    WarnInMenu("Код подключения не подходит", "По этому адресу другой сервер. Попросите у хоста код ещё раз");
                    break;
                default:
                    WarnInMenu("Сервер не отвечает", "Проверьте код подключения и что сервер запущен");
                    break;
            }
        }

        private void WarnInMenu(string title, string details)
        {
            warning = title;
            warningDetails = details;
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

        private string Address(NetworkManager network, bool hosting)
        {
            UnityTransport transport = network.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Log.Warn("No unity transport to configure");
                return null;
            }

            transport.UseEncryption = true;

            if (hosting)
            {
                ServerConfig server = Config.Read().Server;
                transport.SetConnectionData(AnyAddress, server.Port, AnyAddress);

                Account account = Config.Account;
                transport.SetServerSecrets(account.Certificate, account.PrivateKeyPem);
                Log.Info($"World listens on port {server.Port}, channel encrypted");
                return account.Certificate;
            }

            ClientConfig client = Config.Read().Client;
            if (!Invite.TryDecode(client.Invite, out string address, out ushort port, out string certificate))
            {
                Log.Warn("Invite code is missing or malformed, staying in the menu");
                menu.Warn("Неверный код подключения", "Попросите у хоста код ещё раз и вставьте его целиком");
                return null;
            }

            transport.SetConnectionData(address, port);
            transport.SetClientSecrets(Account.CommonName, certificate);
            Log.Info($"Heading for {address}:{port}, channel encrypted");
            return certificate;
        }

        // Spawned before the map loads, so no scene has to carry it: Netcode takes a spawned object along through the
        // scene change. On the host the map wakes up into a world that already has its state. A client loads the map
        // first and gets the state after it, together with the other network objects
        private bool State()
        {
            GameObject prefab = Resources.Load<GameObject>(StatePrefab);
            if (prefab == null)
            {
                Log.Error($"No {StatePrefab} prefab in Resources, refusing to start");
                return false;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = StatePrefab;
            instance.GetComponent<NetworkObject>().Spawn();
            Log.Info("Game state is up");
            return true;
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
