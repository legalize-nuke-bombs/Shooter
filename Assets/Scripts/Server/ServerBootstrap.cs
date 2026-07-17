using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Server
{
    public static class ServerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isBatchMode) return;

            var go = new GameObject("ServerHost");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ServerHost>();

            if (SceneManager.GetActiveScene().name != "Game")
                SceneManager.LoadScene("Game");
        }
    }
}
