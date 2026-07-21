using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;
using System;

namespace Shooter.Server
{
    public static class ServerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isBatchMode) return;

            Log.ToFile(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "shooter-server.log"));
            Log.Info("Bootstrapping server...");

            var go = new GameObject("ServerHost");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ServerHost>();

            if (SceneManager.GetActiveScene().name != "Game")
                SceneManager.LoadScene("Game");
        }
    }
}
