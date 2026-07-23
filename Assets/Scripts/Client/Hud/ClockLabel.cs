using UnityEngine;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Time;

namespace Shooter.Client.Hud
{
    public class ClockLabel : UiElement
    {
        private readonly ClientWorld world;
        private readonly TextLine line;

        public ClockLabel(Font font, ClientWorld world)
        {
            this.world = world;
            style.top = 12;
            style.right = 16;

            line = new TextLine(font, 15);
            line.style.unityTextAlign = TextAnchor.MiddleRight;
            Add(line);
        }

        protected override void OnTick(float dt)
        {
            if (world.Clock == null)
            {
                Visible = false;
                return;
            }

            long timestamp = world.Clock.Timestamp;
            int minutes = (int)(DayCycle.FractionOf(timestamp) * 1440f);
            Visible = true;
            line.text = $"День {DayCycle.DayOf(timestamp) + 1}, {minutes / 60:D2}:{minutes % 60:D2}";
        }
    }
}
