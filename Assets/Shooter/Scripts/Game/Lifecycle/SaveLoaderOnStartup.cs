using System;
using System.Collections;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Shooter.Game
{
    public class SaveLoaderOnStartup : ShooterBehaviour
    {
        private const string BrokenSave = "Не удалось загрузить сохранение";
        private const string BrokenSaveDetails = "Файл повреждён или несовместим с этой версией";
        private static readonly Journal Log = Logs.Here();

        private NetworkManager network;
        private NetworkStarter starter;
        private LoadingScreenOverlay loading;
        private FrozenWorld frozen;

        public event Action Loaded;

        protected override void ShooterAwake()
        {
            network = NetworkManager.Singleton;
            if (!network.IsServer) return;

            starter = network.GetComponent<NetworkStarter>();
            loading = FindAnyObjectByType<LoadingScreenOverlay>();
            SaveManager.Open();
            network.SceneManager.OnLoadComplete += SceneLoaded;
        }

        protected override void ShooterOnDestroy()
        {
            if (network.SceneManager != null) network.SceneManager.OnLoadComplete -= SceneLoaded;
        }

        private void SceneLoaded(ulong client, string scene, LoadSceneMode mode)
        {
            if (client != NetworkManager.ServerClientId || scene != gameObject.scene.name) return;

            network.SceneManager.OnLoadComplete -= SceneLoaded;

            // Netcode calls this before the scene's first Update: freezing here keeps that one Update from running on
            // a world the save has not loaded yet
            if (starter.Save != null) frozen = SaveManager.Freeze();
            StartCoroutine(Begin());
        }

        private IEnumerator Begin()
        {
            yield return null;
            Log.Info($"Scene {gameObject.scene.name} is up");

            if (frozen != null && !Restore()) yield break;

            Loaded?.Invoke();
            loading.Hide();
        }

        private bool Restore()
        {
            Log.Info($"Loading the world from {starter.Save}");
            loading.Show(LoadingScreenStage.Save);
            if (SaveManager.Load(frozen, starter.Save)) return true;

            Log.Warn($"The world failed to load {starter.Save}, shutting the host down");
            starter.Failure = new NetworkStartFailure(BrokenSave, BrokenSaveDetails);
            network.Shutdown();
            return false;
        }
    }
}
