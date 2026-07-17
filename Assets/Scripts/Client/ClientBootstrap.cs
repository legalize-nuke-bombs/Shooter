using UnityEngine;
using UnityEngine.SceneManagement;
using Shooter.Logging;

namespace Shooter.Client
{
    public static class ClientBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (Application.isBatchMode) return;

            SceneManager.sceneLoaded += OnSceneLoaded;
            if (SceneManager.GetActiveScene().name == "Game")
                LoadMap();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Game")
                LoadMap();
        }

        private static void LoadMap()
        {
            if (SceneManager.GetSceneByName("Map").isLoaded) return;
            SceneManager.LoadScene("Map", LoadSceneMode.Additive);
            Log.Info("client: Map loaded additively for render");
        }
    }
}
