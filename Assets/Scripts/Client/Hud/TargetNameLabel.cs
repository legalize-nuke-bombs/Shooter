using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Aiming;
using Shooter.Client.Ui;
using Shooter.Client.Worlds.Entities;

namespace Shooter.Client.Hud
{
    public class TargetNameLabel : UiElement
    {
        private const float Reach = 20f;

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
            EntityView target = aim.TargetView(Reach);
            bool named = target != null && !string.IsNullOrEmpty(target.Name);

            Visible = named;
            if (named) line.text = target.Name;
        }
    }
}
