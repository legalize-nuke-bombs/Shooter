using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Time;

namespace Shooter.Client.Hud
{
    public class ClockLabel : HudLabel
    {
        private readonly ClientWorld world;

        public ClockLabel(Font font, ClientWorld world) : base(font)
        {
            this.world = world;
            style.top = 12;
            style.right = 16;
            style.unityTextAlign = TextAnchor.MiddleRight;
            style.fontSize = 15;
        }

        protected override void Refresh()
        {
            if (world.Clock == null)
            {
                style.display = DisplayStyle.None;
                return;
            }

            long timestamp = world.Clock.Timestamp;
            int minutes = (int)(DayCycle.FractionOf(timestamp) * 1440f);
            style.display = DisplayStyle.Flex;
            text = $"День {DayCycle.DayOf(timestamp) + 1}, {minutes / 60:D2}:{minutes % 60:D2}";
        }
    }
}
