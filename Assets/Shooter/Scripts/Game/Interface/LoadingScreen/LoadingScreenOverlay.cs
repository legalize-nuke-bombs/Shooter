using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class LoadingScreenOverlay : Overlay
    {
        private const string ScreenElementName = "loading";
        private const string StageLabelName = "loading-text";
        private const string TipLabelName = "loading-tip";
        private const string CancelButtonName = "loading-cancel";

        private static readonly Dictionary<LoadingScreenStage, string> StageTitles = new()
        {
            [LoadingScreenStage.Scene] = "Загрузка сцены",
            [LoadingScreenStage.Server] = "Запуск сервера",
            [LoadingScreenStage.Save] = "Загрузка сохранения",
            [LoadingScreenStage.Connection] = "Подключение к серверу",
            [LoadingScreenStage.Synchronization] = "Синхронизация мира"
        };

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private LoadingScreenTipCatalog tipCatalog;

        private Button cancelButton;
        private Action cancelCallback;
        private NetworkManager network;
        private VisualElement screenElement;
        private bool shown;
        private Label stageLabel;
        private string stageTitle;
        private Label tipLabel;

        protected override void ShooterAwake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            network = NetworkManager.Singleton;
            network.OnClientStarted += HandleClientStarted;
            network.OnClientConnectedCallback += HandleClientConnected;
        }

        protected override void ShooterOnDestroy()
        {
            network.OnClientStarted -= HandleClientStarted;
            network.OnClientConnectedCallback -= HandleClientConnected;
        }

        public void ShowStage(LoadingScreenStage stage, Action cancelCallback = null)
        {
            stageTitle = StageTitles[stage];
            this.cancelCallback = cancelCallback;
            shown = true;
            Log.Info($"Loading screen shows {stageTitle}");
            UpdateElementsIfBound();
        }

        public void HideScreen()
        {
            shown = false;
            cancelCallback = null;
            Log.Info("Loading screen is down");
            UpdateElementsIfBound();
        }

        protected override bool TryBindElements(VisualElement root)
        {
            screenElement = root.Q<VisualElement>(ScreenElementName);
            stageLabel = root.Q<Label>(StageLabelName);
            tipLabel = root.Q<Label>(TipLabelName);
            cancelButton = root.Q<Button>(CancelButtonName);

            if (screenElement == null || stageLabel == null || tipLabel == null || cancelButton == null)
            {
                Log.Error($"Overlay document has no complete {ScreenElementName} screen, the loading state stays hidden");
                return false;
            }

            cancelButton.clicked += HandleCancelClicked;
            tipLabel.text = GetRandomTip();
            UpdateElements();

            return true;
        }

        protected override void UnbindElements()
        {
            screenElement = null;
            stageLabel = null;
            tipLabel = null;
            cancelButton = null;
        }

        private void UpdateElementsIfBound()
        {
            if (IsBound) UpdateElements();
        }

        private void UpdateElements()
        {
            screenElement.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            stageLabel.text = stageTitle;
            cancelButton.style.display = cancelCallback != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HandleCancelClicked()
        {
            Log.Info($"The player gives up {stageTitle}");
            cancelCallback();
        }

        // Every start brings a scene manager of its own
        private void HandleClientStarted()
        {
            network.SceneManager.OnSceneEvent += ShowStageOfSceneEvent;
        }

        private void ShowStageOfSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId != network.LocalClientId) return;

            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:
                    ShowStage(LoadingScreenStage.Scene);
                    break;
                case SceneEventType.LoadComplete when !network.IsServer:
                    ShowStage(LoadingScreenStage.Synchronization);
                    break;
            }
        }

        // A client connects only once its world is synchronized, so this is the moment it is in
        private void HandleClientConnected(ulong clientId)
        {
            if (network.IsServer || clientId != network.LocalClientId) return;

            HideScreen();
        }

        private string GetRandomTip()
        {
            if (tipCatalog == null || tipCatalog.Count == 0) return "";

            return tipCatalog.GetAssetAt(Random.Range(0, tipCatalog.Count)).Text;
        }
    }
}
