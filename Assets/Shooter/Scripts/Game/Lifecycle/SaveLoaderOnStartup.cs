using System;
using System.Collections;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Shooter.Game
{
    public class SaveLoaderOnStartup : ShooterBehaviour
    {
        private const string BrokenSaveTitle = "Не удалось загрузить сохранение";
        private const string BrokenSaveDetails = "Файл повреждён или несовместим с этой версией";
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private NetworkManager network;
        private NetworkStarter networkStarter;
        private LoadingScreenOverlay loadingScreenOverlay;
        private FrozenWorld frozenWorld;

        public event Action Loaded;

        protected override void ShooterAwake()
        {
            network = NetworkManager.Singleton;
            if (!network.IsServer) return;

            networkStarter = network.GetComponent<NetworkStarter>();
            loadingScreenOverlay = FindAnyObjectByType<LoadingScreenOverlay>();
            SaveManager.ForgetUnfinishedSave();
            network.SceneManager.OnLoadComplete += HandleSceneLoaded;
        }

        protected override void ShooterOnDestroy()
        {
            if (network.SceneManager != null) network.SceneManager.OnLoadComplete -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (clientId != NetworkManager.ServerClientId || sceneName != gameObject.scene.name) return;

            network.SceneManager.OnLoadComplete -= HandleSceneLoaded;

            // Netcode calls this before the scene's first Update: freezing here keeps that one Update from running on
            // a world the save has not loaded yet
            if (networkStarter.SaveLocationToLoad != null) frozenWorld = SaveManager.FreezeWorld();
            StartCoroutine(LoadWorldNextFrame());
        }

        private IEnumerator LoadWorldNextFrame()
        {
            yield return null;
            Log.Info($"Scene {gameObject.scene.name} is up");

            if (frozenWorld != null && !LoadSaveOrShutDown()) yield break;

            Loaded?.Invoke();
            loadingScreenOverlay.HideScreen();
        }

        private bool LoadSaveOrShutDown()
        {
            Log.Info($"Loading the world from {networkStarter.SaveLocationToLoad}");
            loadingScreenOverlay.ShowStage(LoadingScreenStage.Save);
            if (SaveManager.LoadSaveAndThawWorld(frozenWorld, networkStarter.SaveLocationToLoad)) return true;

            Log.Warn($"The world failed to load {networkStarter.SaveLocationToLoad}, shutting the host down");
            networkStarter.StartFailureToShow = new NetworkStartFailure(BrokenSaveTitle, BrokenSaveDetails);
            network.Shutdown();
            return false;
        }
    }
}
