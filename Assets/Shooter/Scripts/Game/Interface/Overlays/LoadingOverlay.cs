using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Shooter.Game
{
    public class LoadingOverlay : Overlay
    {
        private const string ScreenElement = "loading";
        private const string TextElement = "loading-text";
        private const string TipElement = "loading-tip";
        private const string CancelElement = "loading-cancel";

        private static readonly Dictionary<LoadingStage, string> Titles = new()
        {
            [LoadingStage.Scene] = "Загрузка сцены",
            [LoadingStage.Server] = "Запуск сервера",
            [LoadingStage.Save] = "Загрузка сохранения",
            [LoadingStage.Connection] = "Подключение к серверу",
            [LoadingStage.Synchronization] = "Синхронизация мира"
        };

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private TipCatalog tips;

        private Button cancel;
        private Action cancelled;
        private string caption;
        private VisualElement screen;
        private bool shown;
        private Label text;
        private Label tip;

        // A stage the player may give up shows a cancel button, and the press goes back to whoever started the stage
        public void Show(LoadingStage stage, Action cancelled = null)
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

        private string Tip()
        {
            if (tips == null || tips.Count == 0) return "";

            return tips.At(Random.Range(0, tips.Count)).Text;
        }
    }
}
