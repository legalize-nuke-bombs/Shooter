using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.GameServer
{
    public static class ServerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isBatchMode) return;

            var go = new GameObject("GameServer");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameServer>();

            if (SceneManager.GetActiveScene().name != "Game")
                SceneManager.LoadScene("Game");
        }
    }
}
