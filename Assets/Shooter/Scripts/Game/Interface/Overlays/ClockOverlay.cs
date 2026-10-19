using System;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ClockOverlay : Overlay
    {
        private const string ClockElement = "clock";
        private const long Hidden = long.MinValue;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private Clock clock;
        private Label clockLabel;
        private long shownMinute = Hidden;

        private void Start()
        {
            clock = Registers.GetAll<Clock>(RegisterInactivePolicy.Include).Single();
        }

        private void Update()
        {
            if (!Bound) return;

            // No clock any more for a frame on the way out: the world goes before its scene does
            if (clock == null)
            {
                Hide();
                return;
            }

            DateTime now = clock.Now;
            long minute = now.Ticks / TimeSpan.TicksPerMinute;

            if (minute == shownMinute) return;

            shownMinute = minute;
            clockLabel.text = RussianDate.Moment(now);
        }

        protected override bool Bind(VisualElement root)
        {
            clockLabel = root.Q<Label>(ClockElement);
            shownMinute = Hidden;

            if (clockLabel == null)
            {
                Log.Error($"Overlay document has no {ClockElement} label, the clock stays hidden");
                return false;
            }

            return true;
        }

        protected override void Unbind()
        {
            clockLabel = null;
        }

        private void Hide()
        {
            if (shownMinute == Hidden) return;

            shownMinute = Hidden;
            clockLabel.text = string.Empty;
        }
    }
}
