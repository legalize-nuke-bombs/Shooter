using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Client.Worlds.Entities;

namespace Shooter.Client.Hud
{
    public class HpBar : UiElement
    {
        private const float Thickness = 15f;
        private const float RelOffsetX = 0.2f;
        private const float RelOffsetY = 0.8f;
        private const float RelWidth = 0.2f;

        private static readonly Color TrackColor = new Color(0f, 0f, 0f);
        private static readonly Color FillColor = new Color(0.5f, 0f, 0f);

        private readonly ClientWorld world;

        public HpBar(ClientWorld world)
        {
            this.world = world;
            Fullscreen();
        }

        protected override void OnTick(float dt)
        {
            MarkDirtyRepaint();
        }

        protected override void Draw(Painter2D painter, Rect rect)
        {
            EntityView me = world.Me;
            if (me == null || me.MaxHp <= 0) return;

            float left = rect.width * RelOffsetX;
            float y = rect.height * RelOffsetY;
            float width = rect.width * RelWidth;

            painter.lineWidth = Thickness;
            DrawSegment(painter, TrackColor, left, y, width);
            DrawSegment(painter, FillColor, left, y, width * me.Hp / me.MaxHp);
        }

        private static void DrawSegment(Painter2D painter, Color color, float left, float y, float width)
        {
            if (width <= 0f) return;

            painter.strokeColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(left, y));
            painter.LineTo(new Vector2(left + width, y));
            painter.Stroke();
        }
    }
}
