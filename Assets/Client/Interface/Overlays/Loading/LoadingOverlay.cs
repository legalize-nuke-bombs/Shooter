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
        private const string ProgressElement = "loading-progress";
        private const string FillElement = "loading-progress-fill";
        private const float ActivationShare = 0.9f;

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
        private VisualElement fill;
        private VisualElement progress;
        private VisualElement screen;
        private bool shown;
        private Label text;
        private Label tip;
        private AsyncOperation tracked;

        private void Update()
        {
            if (Bound && tracked != null) Fill();
        }

        public void Show(LoadingStage stage, string detail = null)
        {
            caption = detail == null ? Titles[stage] : Titles[stage] + " — " + detail;
            shown = true;
            tracked = null;
            Log.Info($"Loading screen says: {caption}");
            Render();
        }

        public void Track(AsyncOperation operation)
        {
            tracked = operation;
            Render();
        }

        public void Hide()
        {
            shown = false;
            tracked = null;
            Log.Info("Loading screen is down");
            Render();
        }

        protected override bool Bind(VisualElement root)
        {
            screen = root.Q<VisualElement>(ScreenElement);
            text = root.Q<Label>(TextElement);
            tip = root.Q<Label>(TipElement);
            progress = root.Q<VisualElement>(ProgressElement);
            fill = root.Q<VisualElement>(FillElement);

            if (screen == null || text == null || tip == null || progress == null || fill == null)
            {
                Log.Error($"Overlay document has no complete {ScreenElement} screen, the loading state stays hidden");
                return false;
            }

            tip.text = Tip();
            Render();

            return true;
        }

        protected override void Unbind()
        {
            screen = null;
            text = null;
            tip = null;
            progress = null;
            fill = null;
        }

        private void Render()
        {
            if (!Bound) return;

            screen.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            text.text = caption;
            progress.style.display = tracked == null ? DisplayStyle.None : DisplayStyle.Flex;

            if (tracked != null) Fill();
        }

        private void Fill()
        {
            fill.style.width = Length.Percent(Mathf.Clamp01(tracked.progress / ActivationShare) * 100f);
        }

        private string Tip()
        {
            if (tips == null || tips.Count == 0) return "";

            return tips.At(Random.Range(0, tips.Count)).Text;
        }
    }
}
