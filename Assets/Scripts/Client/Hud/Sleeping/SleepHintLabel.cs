using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Hud.Sleeping
{
    public class SleepHintLabel : Label
    {
        private readonly SleepSense sleepSense;

        public SleepHintLabel(Font font, SleepSense sleepSense)
        {
            this.sleepSense = sleepSense;

            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;
            style.bottom = Length.Percent(18);
            style.unityTextAlign = TextAnchor.MiddleCenter;
            style.fontSize = 14;
            style.color = new Color(0.76f, 0.79f, 0.83f);
            style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
            style.textShadow = new TextShadow
            {
                offset = Vector2.zero,
                blurRadius = 8f,
                color = new Color(0f, 0f, 0f, 0.9f)
            };
            style.display = DisplayStyle.None;

            schedule.Execute(Refresh).Every(16);
        }

        private void Refresh()
        {
            if (sleepSense.MySleeping)
            {
                if (sleepSense.WorldAsleep)
                {
                    style.display = DisplayStyle.None;
                }
                else
                {
                    text = "[E] Встать";
                    style.display = DisplayStyle.Flex;
                }
            }
            else if (sleepSense.CanSleep)
            {
                text = "[E] Спать";
                style.display = DisplayStyle.Flex;
            }
            else
            {
                style.display = DisplayStyle.None;
            }
        }
    }
}
