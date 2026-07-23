using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;

namespace Shooter.Client.Hud.Sleeping
{
    public class SleepHintLabel : UiElement
    {
        private readonly SleepSense sleepSense;
        private readonly TextLine line;

        public SleepHintLabel(Font font, SleepSense sleepSense)
        {
            this.sleepSense = sleepSense;
            style.left = 0;
            style.right = 0;
            style.bottom = Length.Percent(18);

            line = new TextLine(font, 14);
            line.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(line);
        }

        protected override void OnTick(float dt)
        {
            if (sleepSense.MySleeping)
            {
                if (sleepSense.WorldAsleep)
                {
                    Visible = false;
                }
                else
                {
                    line.text = "[E] Встать";
                    Visible = true;
                }
            }
            else if (sleepSense.CanSleep)
            {
                line.text = "[E] Спать";
                Visible = true;
            }
            else
            {
                Visible = false;
            }
        }
    }
}
