using Shooter.Client.Playing;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class SleepOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string SleepElement = "sleep";

        private SleepView view;
        private VisualElement screen;
        private float shown;

        private void Update()
        {
            if (!Bound) return;

            float blink = Blink();
            if (Mathf.Approximately(blink, shown)) return;

            shown = blink;

            if (blink <= 0f)
            {
                screen.style.display = DisplayStyle.None;
                return;
            }

            screen.style.display = DisplayStyle.Flex;
            screen.style.backgroundColor = new Color(0f, 0f, 0f, blink);
        }

        protected override bool Bind(VisualElement root)
        {
            screen = root.Q<VisualElement>(SleepElement);
            shown = 0f;

            if (screen == null)
            {
                Log.Error($"Overlay document has no {SleepElement} element, sleep stays unseen");
                return false;
            }

            screen.style.display = DisplayStyle.None;

            return true;
        }

        protected override void Unbind()
        {
            screen = null;
            view = null;
            shown = 0f;
        }

        private float Blink()
        {
            if (view == null) view = OwnPlayer.Find<SleepView>();

            return view == null ? 0f : view.Blink;
        }
    }
}
