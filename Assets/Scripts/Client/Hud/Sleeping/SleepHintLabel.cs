using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Hud.Sleeping
{
    public class SleepHintLabel : HudLabel
    {
        private readonly SleepSense sleepSense;

        public SleepHintLabel(Font font, SleepSense sleepSense) : base(font)
        {
            this.sleepSense = sleepSense;
            style.left = 0;
            style.right = 0;
            style.bottom = Length.Percent(18);
            style.unityTextAlign = TextAnchor.MiddleCenter;
            style.fontSize = 14;
        }

        protected override void Refresh()
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
