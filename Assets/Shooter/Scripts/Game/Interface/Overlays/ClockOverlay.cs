using System;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ClockOverlay : Overlay, IShooterUpdateable
    {
        private const string ClockLabelName = "clock";
        private const long Hidden = long.MinValue;
        private static readonly Journal Log = Logs.Here();

        private Clock clock;
        private Label clockLabel;
        private long shownMinute = Hidden;

        private void Start()
        {
            clock = Registers.Get<Clock>();
        }

        public void ShooterUpdate()
        {
            if (!IsBound) return;

            // No clock any more for a frame on the way out: the world goes before its scene does
            if (clock == null)
            {
                HideClock();
                return;
            }

            DateTime now = clock.Now;
            long minute = now.Ticks / TimeSpan.TicksPerMinute;

            if (minute == shownMinute) return;

            shownMinute = minute;
            clockLabel.text = RussianDate.FormatDateAndTime(now);
        }

        protected override bool TryBindElements(VisualElement root)
        {
            clockLabel = root.Q<Label>(ClockLabelName);
            shownMinute = Hidden;

            if (clockLabel == null)
            {
                Log.Error($"Overlay document has no {ClockLabelName} label, the clock stays hidden");
                return false;
            }

            return true;
        }

        protected override void UnbindElements()
        {
            clockLabel = null;
        }

        private void HideClock()
        {
            if (shownMinute == Hidden) return;

            shownMinute = Hidden;
            clockLabel.text = string.Empty;
        }
    }
}
