using System.Collections;
using System.Text;
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
        private const string MenuScene = "Menu";
        private const string BootScene = "Boot";
        private const string WorldScene = "Map";
        private const string AnyAddress = "0.0.0.0";
        private const int MenuFrameRate = 60;
        private const int UnlimitedFrameRate = -1;
        private static readonly Journal Log = Logs.Here();
        private bool ending;

        private GameObject overlays;

        private IEnumerator Start()
        {
            Compression();
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

            if (hosting) yield return SceneManager.LoadSceneAsync(WorldScene, LoadSceneMode.Single);

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

            if (!(hosting ? network.StartHost() : network.StartClient()))
            {
                Log.Error($"The {(hosting ? "host" : "client")} refused to start");
                yield return ToMenu();
                yield break;
            }

            Log.Info($"{(hosting ? "Host" : "Client")} is up as {client.Name}");

            if (save != null) Load(save);
        }

        private static void Load(string save)
        {
            SaveManager saves = SaveManager.Current;
            if (saves == null)
            {
                Log.Error($"World has no save manager, {save} stays unloaded");
                return;
            }

            Log.Info($"Loading the world from {save}");
            saves.Load(save);
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

            if (hosting)
            {
                ServerConfig server = Config.Read().Server;
                transport.SetConnectionData(AnyAddress, server.Port, AnyAddress);
                Log.Info($"World listens on port {server.Port}");
                return;
            }

            ClientConfig client = Config.Read().Client;
            transport.SetConnectionData(client.Address, client.Port);
            Log.Info($"Heading for {client.Address}:{client.Port}");
        }

        private void Compression()
        {
            GameObject prefab = Resources.Load<GameObject>(CompressionPrefab);
            if (prefab == null)
            {
                Log.Error($"No {CompressionPrefab} prefab in Resources, saves stay folders");
                return;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = CompressionPrefab;
            DontDestroyOnLoad(instance);
            Log.Info("Compression is up");
        }

        private void Overlays()
        {
            GameObject prefab = Resources.Load<GameObject>(OverlayPrefab);
            if (prefab == null)
            {
                Log.Error($"No {OverlayPrefab} prefab in Resources, the session goes without overlays");
                return;
            }

            overlays = Instantiate(prefab);
            overlays.name = OverlayPrefab;
            DontDestroyOnLoad(overlays);
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
