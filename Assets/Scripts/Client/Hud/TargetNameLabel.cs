using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Aiming;
using Shooter.Client.Ui;
using Shooter.Client.Worlds.Entities;

namespace Shooter.Client.Hud
{
    public class TargetNameLabel : UiElement
    {
        private const float Reach = 20;

        private readonly Aim aim;
        private readonly TextLine line;

        public TargetNameLabel(Font font, Aim aim)
        {
            this.aim = aim;
            style.left = 0;
            style.right = 0;
            style.top = Length.Percent(50);
            style.marginTop = 24;

            line = new TextLine(font, 15);
            line.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(line);
        }

        protected override void OnTick(float dt)
        {
            Visible = false;

            RaycastHit? target = aim.Target;

            if (target == null || target.Value.distance > Reach)
            {
                return;
            }

            EntityBody bridge = target.Value.collider.GetComponentInParent<EntityBody>();
            if (bridge != null && !string.IsNullOrEmpty(bridge.View.Name))
            {
                Visible = true;
                line.text = bridge.View.Name;
            }
        }
    }
}
