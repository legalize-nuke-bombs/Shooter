using System;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class ClockOverlay : Overlay
    {
        private const string ClockElement = "clock";
        private const long Hidden = long.MinValue;
        private static readonly Journal Log = Logs.Here();

        private Label clock;
        private long shown = Hidden;

        private void Update()
        {
            if (!Bound) return;

            Clock world = Clock.Current;

            if (world == null)
            {
                Hide();
                return;
            }

            DateTime now = world.Now;
            long minute = now.Ticks / TimeSpan.TicksPerMinute;

            if (minute == shown) return;

            shown = minute;
            clock.text = RussianDate.Moment(now);
        }

        protected override bool Bind(VisualElement root)
        {
            clock = root.Q<Label>(ClockElement);
            shown = Hidden;

            if (clock == null)
            {
                Log.Error($"Overlay document has no {ClockElement} label, the clock stays hidden");
                return false;
            }

            return true;
        }

        protected override void Unbind()
        {
            clock = null;
        }

        private void Hide()
        {
            if (shown == Hidden) return;

            shown = Hidden;
            clock.text = string.Empty;
        }
    }
}
