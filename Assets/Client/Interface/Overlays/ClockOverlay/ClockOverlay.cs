using System;
using System.Globalization;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using Environment = Shooter.Game.Environment;

namespace Shooter.Client.Interface.Overlays
{
    [RequireComponent(typeof(PanelRenderer))]
    public class ClockOverlay : MonoBehaviour
    {
        private const string ClockElement = "clock";
        private const string TimeFormat = "HH:mm";
        private const long Hidden = long.MinValue;

        private static readonly string[] Months =
        {
            "января", "февраля", "марта", "апреля", "мая", "июня",
            "июля", "августа", "сентября", "октября", "ноября", "декабря"
        };

        private PanelRenderer panel;
        private Label clock;
        private long shown = Hidden;

        private void OnEnable()
        {
            panel = GetComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(Bind);
        }

        private void OnDisable()
        {
            panel.UnregisterUIReloadCallback(Bind);
            clock = null;
        }

        private void Update()
        {
            if (clock == null) return;

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

        private void Bind(PanelRenderer renderer, VisualElement root)
        {
            clock = root.Q<Label>(ClockElement);
            shown = Hidden;

            if (clock == null) Log.Error("Overlay document has no {} label, the clock stays hidden", ClockElement);
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
