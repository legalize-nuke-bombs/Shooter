using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities.Parts.Health;

namespace Shooter.Client.Hud
{
    public class DeadScreen : HudLabel
    {
        private readonly ClientWorld world;

        public DeadScreen(Font font, ClientWorld world) : base(font)
        {
            this.world = world;
            style.unityTextAlign = TextAnchor.MiddleCenter;
            style.fontSize = 30;
        }

        protected override void Refresh()
        {
            HealthState health = world.Me.Part<HealthState>();
            if (health == null || (health.Hp > 0))
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;
            text = $"Вы мертвы";
        }
    }
}
