using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class SleepOverlay : Overlay, IShooterUpdateable
    {
        private const string ScreenElementName = "sleep";
        private static readonly Journal Log = Logs.Here();

        private VisualElement screenElement;
        private float shownEyesClosedAmount;
        private SleepView sleepView;

        public void ShooterUpdate()
        {
            if (!IsBound) return;

            float eyesClosedAmount = GetLocalEyesClosedAmount();
            if (Mathf.Approximately(eyesClosedAmount, shownEyesClosedAmount)) return;

            shownEyesClosedAmount = eyesClosedAmount;

            if (eyesClosedAmount <= 0f)
            {
                screenElement.style.display = DisplayStyle.None;
                return;
            }

            screenElement.style.display = DisplayStyle.Flex;
            screenElement.style.backgroundColor = new Color(0f, 0f, 0f, eyesClosedAmount);
        }

        protected override bool TryBindElements(VisualElement root)
        {
            screenElement = root.Q<VisualElement>(ScreenElementName);
            shownEyesClosedAmount = 0f;

            if (screenElement == null)
            {
                Log.Error($"Overlay document has no {ScreenElementName} element, sleep stays unseen");
                return false;
            }

            screenElement.style.display = DisplayStyle.None;

            return true;
        }

        protected override void UnbindElements()
        {
            screenElement = null;
            sleepView = null;
            shownEyesClosedAmount = 0f;
        }

        private float GetLocalEyesClosedAmount()
        {
            if (sleepView == null) sleepView = LocalPlayerFinder.FindComponent<SleepView>();

            return sleepView == null ? 0f : sleepView.EyesClosedAmount;
        }
    }
}
