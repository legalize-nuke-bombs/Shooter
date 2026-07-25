using System;
using System.IO;
using Unity.Multiplayer;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;

namespace Shooter.Bootstrapping
{
    public static class Bootstrap
    {
        private const string ServerArgument = "-server";
        private const string ClientArgument = "-client";
        private const string NetworkPrefab = "NetworkManager";
        private const string WorldScene = "Map";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (ServerRequested()) StartServer();
            else StartClient();
        }

        private static bool ServerRequested()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument == ServerArgument) return true;
                if (argument == ClientArgument) return false;
            }

            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
            if (role == MultiplayerRoleFlags.Server) return true;
            if (role == MultiplayerRoleFlags.Client) return false;

            return Application.isBatchMode;
        }

        private static void StartServer()
        {
            Log.ToFile(InHome("shooter-server.log"));
            Log.Info("Bootstrapping server...");

            NetworkManager network = Network();
            if (network == null) return;

            if (!network.StartServer())
            {
                Log.Error("Server refused to start");
                return;
            }

            Log.Info("Server listening, loading world scene {}", WorldScene);
            network.SceneManager.LoadScene(WorldScene, LoadSceneMode.Single);
        }

        private static void StartClient()
        {
            Log.ToFile(InHome("shooter-client.log"));
            Log.Info("Bootstrapping client...");

            NetworkManager network = Network();
            if (network == null) return;

            if (!network.StartClient())
            {
                Log.Error("Client refused to start");
                return;
            }

            Log.Info("Client connecting...");
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
    }
}
