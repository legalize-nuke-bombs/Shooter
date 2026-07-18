using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Aiming;
using Shooter.Client.Entities.Npcs;

namespace Shooter.Client.Hud
{
    public class TargetNameLabel : Label
    {
        private readonly Aim aim;

        public TargetNameLabel(Font font, Aim aim)
        {
            this.aim = aim;

            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;
            style.top = Length.Percent(50);
            style.marginTop = 24;
            style.unityTextAlign = TextAnchor.MiddleCenter;
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
            NpcAvatar target = aim.Target;
            bool targeted = target != null && !string.IsNullOrEmpty(target.Name);
            style.display = targeted ? DisplayStyle.Flex : DisplayStyle.None;
            if (targeted) text = target.Name;
        }
    }
}
