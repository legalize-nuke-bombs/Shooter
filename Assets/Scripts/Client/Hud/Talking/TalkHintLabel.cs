using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;

namespace Shooter.Client.Hud.Talking
{
    public class TalkHintLabel : UiElement
    {
        private readonly TalkSense talkSense;
        private readonly TextLine line;

        public TalkHintLabel(Font font, TalkSense talkSense)
        {
            this.talkSense = talkSense;
            style.left = 0;
            style.right = 0;
            style.bottom = Length.Percent(18);

            line = new TextLine(font, 14);
            line.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(line);
        }

        protected override void OnTick(float dt)
        {
            if (talkSense.TalkerTargeted())
            {
                line.text = "[P] Говорить";
                Visible = true;
            }
            else
            {
                Visible = false;
            }
        }
    }
}
