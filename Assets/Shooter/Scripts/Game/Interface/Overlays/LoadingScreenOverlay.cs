using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Shooter.Game
{
    // Stands in Boot and lives through every scene. The menu and the world show the stages they start and take the
    // screen down; the loads Netcode runs between them the screen follows itself, as nobody else is up to see them
    public class LoadingScreenOverlay : Overlay
    {
        private const string ScreenElement = "loading";
        private const string TextElement = "loading-text";
        private const string TipElement = "loading-tip";
        private const string CancelElement = "loading-cancel";

        private static readonly Dictionary<LoadingScreenStage, string> Titles = new()
        {
            [LoadingScreenStage.Scene] = "Загрузка сцены",
            [LoadingScreenStage.Server] = "Запуск сервера",
            [LoadingScreenStage.Save] = "Загрузка сохранения",
            [LoadingScreenStage.Connection] = "Подключение к серверу",
            [LoadingScreenStage.Synchronization] = "Синхронизация мира"
        };

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private TipCatalog tips;

        private Button cancel;
        private Action cancelled;
        private string caption;
        private NetworkManager network;
        private VisualElement screen;
        private bool shown;
        private Label text;
        private Label tip;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            network = NetworkManager.Singleton;
            network.OnClientStarted += Watch;
        }

        private void OnDestroy()
        {
            network.OnClientStarted -= Watch;
        }

        // A stage the player may give up shows a cancel button, and the press goes back to whoever started the stage
        public void Show(LoadingScreenStage stage, Action cancelled = null)
        {
            caption = Titles[stage];
            this.cancelled = cancelled;
            shown = true;
            Log.Info($"Loading screen shows {caption}");
            Apply();
        }

        public void Hide()
        {
            shown = false;
            cancelled = null;
            Log.Info("Loading screen is down");
            Apply();
        }

        protected override bool Bind(VisualElement root)
        {
            screen = root.Q<VisualElement>(ScreenElement);
            text = root.Q<Label>(TextElement);
            tip = root.Q<Label>(TipElement);
            cancel = root.Q<Button>(CancelElement);

            if (screen == null || text == null || tip == null || cancel == null)
            {
                Log.Error($"Overlay document has no complete {ScreenElement} screen, the loading state stays hidden");
                return false;
            }

            cancel.clicked += Cancel;
            tip.text = Tip();
            Paint();

            return true;
        }

        protected override void Unbind()
        {
            screen = null;
            text = null;
            tip = null;
            cancel = null;
        }

        private void Apply()
        {
            if (Bound) Paint();
        }

        private void Paint()
        {
            screen.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            text.text = caption;
            cancel.style.display = cancelled != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Cancel()
        {
            Log.Info($"The player gives up {caption}");
            cancelled();
        }

        // Every start brings a scene manager of its own
        private void Watch()
        {
            network.SceneManager.OnSceneEvent += Follow;
        }

        private void Follow(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId != network.LocalClientId) return;

            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:
                    Show(LoadingScreenStage.Scene);
                    break;
                case SceneEventType.LoadComplete when !network.IsServer:
                    Show(LoadingScreenStage.Synchronization);
                    break;
            }
        }

        private string Tip()
        {
            if (tips == null || tips.Count == 0) return "";

            return tips.At(Random.Range(0, tips.Count)).Text;
        }
    }
}
