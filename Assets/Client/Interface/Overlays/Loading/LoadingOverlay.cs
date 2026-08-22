using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class LoadingOverlay : Overlay
    {
        private const string ScreenElement = "loading";
        private const string TextElement = "loading-text";
        private const string TipElement = "loading-tip";

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

        private string caption;
        private VisualElement screen;
        private bool shown;
        private Label text;
        private Label tip;

        public void Show(LoadingStage stage)
        {
            caption = Titles[stage];
            shown = true;
            Log.Info($"Loading screen shows {caption}");
            Apply();
        }

        public void Hide()
        {
            shown = false;
            Log.Info("Loading screen is down");
            Apply();
        }

        protected override bool Bind(VisualElement root)
        {
            screen = root.Q<VisualElement>(ScreenElement);
            text = root.Q<Label>(TextElement);
            tip = root.Q<Label>(TipElement);

            if (screen == null || text == null || tip == null)
            {
                Log.Error($"Overlay document has no complete {ScreenElement} screen, the loading state stays hidden");
                return false;
            }

            tip.text = Tip();
            Paint();

            return true;
        }

        protected override void Unbind()
        {
            screen = null;
            text = null;
            tip = null;
        }

        private void Apply()
        {
            if (Bound) Paint();
        }

        private void Paint()
        {
            screen.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            text.text = caption;
        }

        private string Tip()
        {
            if (tips == null || tips.Count == 0) return "";

            return tips.At(Random.Range(0, tips.Count)).Text;
        }
    }
}
