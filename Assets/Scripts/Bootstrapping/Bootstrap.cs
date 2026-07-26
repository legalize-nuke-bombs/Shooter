using System;
using System.IO;
using System.Text;
using Unity.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Client.Overlays;
using Shooter.Configuring;
using Shooter.Logging;

namespace Shooter.Bootstrapping
{
    public static class Bootstrap
    {
        private const string ServerArgument = "-server";
        private const string ClientArgument = "-client";
        private const string HostArgument = "-host";
        private const string NetworkPrefab = "NetworkManager";
        private const string WorldScene = "Map";
        private const string AnyAddress = "0.0.0.0";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            switch (Role())
            {
                case Part.Server:
                    Start("server", network => network.StartServer(), true);
                    break;
                case Part.Client:
                    Start("client", network => network.StartClient(), false);
                    break;
                default:
                    Start("host", network => network.StartHost(), true);
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
            Log.ToFile(InHome("shooter-" + part + ".log"));
            Log.Info("Bootstrapping {}...", part);

            NetworkManager network = Network();
            if (network == null) return;

            Address(network, hosting);
            network.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(PlayerName());

            if (!begin(network))
            {
                Log.Error("The {} refused to start", part);
                return;
            }

            if (network.IsClient) Overlay();

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
                ServerConfig server = Config.Read<ServerConfig>(ServerConfig.FileName);
                transport.SetConnectionData(AnyAddress, server.Port, AnyAddress);
                Log.Info("World {} listens on port {}", server.World, server.Port);
                return;
            }

            ClientConfig client = Config.Read<ClientConfig>(ClientConfig.FileName);
            transport.SetConnectionData(client.Address, client.Port);
            Log.Info("Heading for {}:{}", client.Address, client.Port);
        }

        private static string PlayerName()
        {
            return Config.Read<ClientConfig>(ClientConfig.FileName).Name;
        }

        private static void Overlay()
        {
            var overlay = new GameObject("VersionOverlay");
            overlay.AddComponent<VersionOverlay>();
            UnityEngine.Object.DontDestroyOnLoad(overlay);
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
