using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities.Chronology;

namespace Shooter.Client.Hud
{
    public class ClockLabel : Label
    {
        public ClockLabel(Font font)
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.top = 12;
            style.right = 16;
            style.unityTextAlign = TextAnchor.MiddleRight;
            style.fontSize = 15;
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
            ClientWorld world = NetworkClient.Instance?.World;
            if (world?.Clock == null)
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
