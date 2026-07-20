using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;

namespace Shooter.Server
{
    public static class ServerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isBatchMode) return;

            Log.ToFile(System.IO.Path.Combine(Application.persistentDataPath, "shooter-server.log"));
            Log.Info("Bootstrapping server...");

            var go = new GameObject("ServerHost");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ServerHost>();

            if (SceneManager.GetActiveScene().name != "Game")
                SceneManager.LoadScene("Game");
        }
    }
}
