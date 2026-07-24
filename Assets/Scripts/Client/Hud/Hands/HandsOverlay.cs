using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Client.Worlds.Entities;
using Shooter.Server.Worlds.Items.Firearm;

namespace Shooter.Client.Hud.Hands
{
    public class HandsOverlay : UiElement
    {
        private const float BarrelWidth = 50f;

        private static readonly Color FirearmColor = new Color(0f, 0f, 0f);

        private readonly ClientWorld world;

        public HandsOverlay(ClientWorld world)
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
            if (me == null || !(me.Equipped is FirearmState)) return;

            painter.strokeColor = FirearmColor;
            painter.lineWidth = BarrelWidth;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.width * 0.9f, rect.height * 0.9f));
            painter.LineTo(new Vector2(rect.width, rect.height));
            painter.Stroke();
        }
    }
}
