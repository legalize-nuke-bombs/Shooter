using System.Collections;
using System.Text;
using Shooter.Client.Interface.Menu;
using Shooter.Configuring;
using Shooter.Logging;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Bootstrapping
{
    internal class Session : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private const string NetworkPrefab = "NetworkManager";
        private const string OverlayPrefab = "Overlays";
        private const string MenuScene = "Menu";
        private const string BootScene = "Boot";
        private const string WorldScene = "Map";
        private const string AnyAddress = "0.0.0.0";

        private GameObject overlays;
        private bool ending;

        private IEnumerator Start()
        {
            yield return ToMenu();
        }

        private IEnumerator ToMenu()
        {
            Drop();

            yield return SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);

            var screen = FindAnyObjectByType<MenuScreen>();
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

        private void Host()
        {
            StartCoroutine(Begin(true));
        }

        private void Join()
        {
            StartCoroutine(Begin(false));
        }

        private void Quit()
        {
            Log.Info("Leaving the game by the player's own hand");
            Application.Quit();
        }

        private IEnumerator Begin(bool hosting)
        {
            // The menu goes first and only then the heavy scene starts loading: a single load keeps the
            // old scene alive until the new one is ready, and the two would sit in memory side by side.
            yield return SceneManager.LoadSceneAsync(BootScene, LoadSceneMode.Single);

            Overlays();

            // And the world has to stand before the network spawns anyone into it: a player born in the
            // empty boot scene has no ground under him and falls while the map is still loading. A joining
            // client waits in the boot scene instead — the server sends him the world itself.
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

            // Netcode is still finishing its own shutdown inside this callback, so the manager is torn
            // down a frame later, when nothing of it is running any more.
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
            var transport = network.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Log.Warn("No unity transport to configure");
                return;
            }

            if (hosting)
            {
                ServerConfig server = Config.Read().Server;
                transport.SetConnectionData(AnyAddress, server.Port, AnyAddress);
                Log.Info($"World {server.World} listens on port {server.Port}");
                return;
            }

            ClientConfig client = Config.Read().Client;
            transport.SetConnectionData(client.Address, client.Port);
            Log.Info($"Heading for {client.Address}:{client.Port}");
        }

        private void Overlays()
        {
            var prefab = Resources.Load<GameObject>(OverlayPrefab);
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

            var prefab = Resources.Load<GameObject>(NetworkPrefab);
            if (prefab == null)
            {
                Log.Error($"No {NetworkPrefab} prefab in Resources, refusing to start");
                return null;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = NetworkPrefab;
            DontDestroyOnLoad(instance);

            var network = instance.GetComponent<NetworkManager>();
            if (network == null) Log.Error($"Prefab {NetworkPrefab} has no NetworkManager component");

            return network;
        }
    }
}
