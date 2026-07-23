using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities.Parts.Health;

namespace Shooter.Client.Hud
{
    public class DeadScreen : UiElement
    {
        private readonly ClientWorld world;

        public DeadScreen(Font font, ClientWorld world)
        {
            this.world = world;
            style.left = 0;
            style.right = 0;
            style.top = Length.Percent(45);

            var line = new TextLine(font, 30);
            line.style.unityTextAlign = TextAnchor.MiddleCenter;
            line.text = "Вы мертвы";
            Add(line);
        }

        protected override void OnTick(float dt)
        {
            HealthState health = world.Me?.Part<HealthState>();
            Visible = health != null && health.Hp == 0;
        }
    }
}
