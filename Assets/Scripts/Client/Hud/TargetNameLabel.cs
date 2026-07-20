using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Aiming;
using Shooter.Client.Worlds.Entities;

namespace Shooter.Client.Hud
{
    public class TargetNameLabel : HudLabel
    {
        private const float Reach = 20;

        private readonly Aim aim;

        public TargetNameLabel(Font font, Aim aim) : base(font)
        {
            this.aim = aim;
            style.left = 0;
            style.right = 0;
            style.top = Length.Percent(50);
            style.marginTop = 24;
            style.unityTextAlign = TextAnchor.MiddleCenter;
            style.fontSize = 15;
        }

        protected override void Refresh()
        {
            style.display = DisplayStyle.None;

            RaycastHit? target = aim.Target;

            if (target == null || target.Value.distance > Reach)
            {
                return;
            }

            EntityBody bridge = target.Value.collider.GetComponentInParent<EntityBody>();
            if (bridge != null && !string.IsNullOrEmpty(bridge.View.Name))
            {
                style.display = DisplayStyle.Flex;
                text = bridge.View.Name;
            }
        }
    }
}
