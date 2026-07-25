using System;
using System.IO;
using System.Text;
using Unity.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;

namespace Shooter.Bootstrapping
{
    public static class Bootstrap
    {
        private const string ServerArgument = "-server";
        private const string ClientArgument = "-client";
        private const string HostArgument = "-host";
        private const string AddressArgument = "-address";
        private const string NameArgument = "-name";
        private const string NetworkPrefab = "NetworkManager";
        private const string WorldScene = "Map";
        private const ushort DefaultPort = 7777;

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

            Address(network);
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

        private static void Address(NetworkManager network)
        {
            string address = Argument(AddressArgument);
            if (string.IsNullOrEmpty(address)) return;

            var transport = network.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Log.Warn("No unity transport to point at {}", address);
                return;
            }

            string[] parts = address.Split(':');
            ushort port = parts.Length > 1 && ushort.TryParse(parts[1], out ushort given) ? given : DefaultPort;

            transport.SetConnectionData(parts[0], port);
            Log.Info("Transport points at {}:{}", parts[0], port);
        }

        private static string PlayerName()
        {
            string given = Argument(NameArgument);
            return string.IsNullOrEmpty(given) ? SystemInfo.deviceName : given;
        }

        private static string Argument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();

            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == name) return arguments[index + 1];
            }

            return null;
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
