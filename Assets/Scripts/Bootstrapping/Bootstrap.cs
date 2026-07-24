using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Client;
using Shooter.Client.Account;
using Shooter.Logging;
using Shooter.Serialization;
using Shooter.Server;

namespace Shooter.Bootstrapping
{
    public static class Bootstrap
    {
        private const string ServerArgument = "-server";
        private const string ClientArgument = "-client";
        private const string ConfigFileName = "config.json";
        private const string GameScene = "Game";
        private const string DefaultServerAddress = "localhost:8080";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (ServerRequested()) StartServer();
            else StartClient();
        }

        private static bool ServerRequested()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            foreach (string argument in arguments)
            {
                if (argument == ServerArgument) return true;
                if (argument == ClientArgument) return false;
            }

            return Application.isBatchMode;
        }

        private static void StartServer()
        {
            Log.ToFile(InHome("shooter-server.log"));
            Log.Info("Bootstrapping server...");

            Host<ServerHost>("ServerHost");

            if (SceneManager.GetActiveScene().name != GameScene)
                SceneManager.LoadScene(GameScene);
        }

        private static void StartClient()
        {
            Log.ToFile(InHome("shooter-client.log"));
            Log.Info("Bootstrapping client...");

            var session = new ClientSession(ReadServerAddress());
            Log.Info("Client talks to {}", session.ServerAddress);

            Host<ClientHost>("ClientHost").Bind(session);
        }

        private static T Host<T>(string name) where T : Component
        {
            var host = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(host);
            return host.AddComponent<T>();
        }

        private static string ReadServerAddress()
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
                if (!File.Exists(path))
                {
                    Log.Warn("No {} in streaming assets, falling back to {}", ConfigFileName, DefaultServerAddress);
                    return DefaultServerAddress;
                }

                var config = Json.Deserialize<ClientConfig>(File.ReadAllText(path));
                string address = config?.ServerAddress?.Trim();
                if (string.IsNullOrEmpty(address))
                {
                    Log.Warn("{} has no server address, falling back to {}", ConfigFileName, DefaultServerAddress);
                    return DefaultServerAddress;
                }

                return address;
            }
            catch (Exception e)
            {
                Log.Warn("Failed to read {}, falling back to {}: {}", ConfigFileName, DefaultServerAddress, e.Message);
                return DefaultServerAddress;
            }
        }

        private static string InHome(string fileName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fileName);
        }
    }
}
