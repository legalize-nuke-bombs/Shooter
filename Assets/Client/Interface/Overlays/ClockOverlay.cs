using System;
using System.Globalization;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using Environment = Shooter.Game.Environment;

namespace Shooter.Client.Interface.Overlays
{
    public class ClockOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string ClockElement = "clock";
        private const string TimeFormat = "HH:mm";
        private const long Hidden = long.MinValue;

        private static readonly string[] Months =
        {
            "января", "февраля", "марта", "апреля", "мая", "июня",
            "июля", "августа", "сентября", "октября", "ноября", "декабря"
        };

        private Label clock;
        private long shown = Hidden;

        private void Update()
        {
            if (!Bound) return;

            Environment environment = Environment.Current;

            if (environment == null)
            {
                Hide();
                return;
            }

            DateTimeOffset now = environment.Clock.Now;
            long minute = now.Ticks / TimeSpan.TicksPerMinute;

            if (minute == shown) return;

            shown = minute;
            clock.text = Describe(now);
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

        private static string Describe(DateTimeOffset now)
        {
            return now.Day + " " + Months[now.Month - 1] + " " + now.Year + ", " + now.ToString(TimeFormat, CultureInfo.InvariantCulture);
        }
    }
}
