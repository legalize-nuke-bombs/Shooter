using System;
using System.Collections;
using System.IO;
using System.Text;
using Shooter.Configuring;
using Shooter.Logging;
using Unity.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Bootstrapping
{
    public static class Bootstrap
    {
        private const string ServerArgument = "-server";
        private const string ClientArgument = "-client";
        private const string HostArgument = "-host";
        private const string NetworkPrefab = "NetworkManager";
        private const string OverlayPrefab = "Overlays";
        private const string WorldScene = "Map";
        private const string AnyAddress = "0.0.0.0";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            // Netcode fills its serializer tables from a generated method in the same startup phase,
            // and the order between assemblies is not defined. Waiting a frame lets it finish first.
            var starter = new GameObject(nameof(Bootstrap));
            starter.AddComponent<Starter>();
            UnityEngine.Object.DontDestroyOnLoad(starter);
        }

        internal static IEnumerator Begin()
        {
            Part part = Role();
            string name = part == Part.Server ? "server" : part == Part.Client ? "client" : "host";

            Log.ToFile(InHome("shooter-" + name + ".log"));
            Log.Info("Bootstrapping {}...", name);

            // Overlays come up first of all: the loading screen has to be on screen while the world
            // loads and while we connect, not after everything is already standing.
            if (part == Part.Server) Listen();
            else Overlay();

            // And the world has to stand before the network spawns anyone into it: a player born in
            // the empty boot scene has no ground under him and falls while the map is still loading.
            if (part != Part.Client && SceneManager.GetActiveScene().name != WorldScene)
            {
                Log.Info("Loading world scene {} before the network starts", WorldScene);
                yield return SceneManager.LoadSceneAsync(WorldScene, LoadSceneMode.Single);
            }

            switch (part)
            {
                case Part.Server:
                    Start(name, network => network.StartServer(), true);
                    break;
                case Part.Client:
                    Start(name, network => network.StartClient(), false);
                    break;
                default:
                    Start(name, network => network.StartHost(), true);
                    break;
            }
        }

        private static Part Role()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument == ServerArgument) return Part.Server;
                if (argument == ClientArgument) return Part.Client;
                if (argument == HostArgument) return Part.Host;
            }

            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
            if (role == MultiplayerRoleFlags.Server) return Part.Server;
            if (role == MultiplayerRoleFlags.Client) return Part.Client;

            return Application.isBatchMode ? Part.Server : Part.Host;
        }

        private static void Start(string part, Func<NetworkManager, bool> begin, bool hosting)
        {
            NetworkManager network = Network();
            if (network == null) return;

            Address(network, hosting);
            network.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(PlayerName());

            if (!begin(network))
            {
                Log.Error("The {} refused to start", part);
                return;
            }

            if (!hosting)
            {
                Log.Info("Connecting as {}...", PlayerName());
                return;
            }

            if (SceneManager.GetActiveScene().name == WorldScene)
            {
                Log.Info("The {} is up, world scene {} is already active", part, WorldScene);
                return;
            }

            Log.Info("The {} is up, loading world scene {}", part, WorldScene);
            network.SceneManager.LoadScene(WorldScene, LoadSceneMode.Single);
        }

        private static void Address(NetworkManager network, bool hosting)
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
                Log.Info("World {} listens on port {}", server.World, server.Port);
                return;
            }

            ClientConfig client = Config.Read().Client;
            transport.SetConnectionData(client.Address, client.Port);
            Log.Info("Heading for {}:{}", client.Address, client.Port);
        }

        private static string PlayerName()
        {
            return Config.Read().Client.Name;
        }

        private static void Overlay()
        {
            var prefab = Resources.Load<GameObject>(OverlayPrefab);
            if (prefab == null)
            {
                Log.Error("No {} prefab in Resources, the client goes without overlays", OverlayPrefab);
                return;
            }

            GameObject overlay = UnityEngine.Object.Instantiate(prefab);
            overlay.name = OverlayPrefab;
            UnityEngine.Object.DontDestroyOnLoad(overlay);
            Log.Info("Overlays are up");
        }

        private static void Listen()
        {
            if (!Application.isEditor) return;

            var listener = new GameObject(nameof(AudioListener));
            listener.AddComponent<AudioListener>();
            UnityEngine.Object.DontDestroyOnLoad(listener);
            Log.Info("Editor-hosted server carries a dummy audio listener, real server builds strip audio entirely");
        }

        private static NetworkManager Network()
        {
            if (NetworkManager.Singleton != null) return NetworkManager.Singleton;

            var prefab = Resources.Load<GameObject>(NetworkPrefab);
            if (prefab == null)
            {
                Log.Error("No {} prefab in Resources, refusing to start", NetworkPrefab);
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = NetworkPrefab;
            UnityEngine.Object.DontDestroyOnLoad(instance);

            var network = instance.GetComponent<NetworkManager>();
            if (network == null) Log.Error("Prefab {} has no NetworkManager component", NetworkPrefab);

            return network;
        }

        private static string InHome(string fileName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fileName);
        }

        private enum Part
        {
            Host,
            Server,
            Client
        }
    }
}
