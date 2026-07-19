using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Hud
{
    public abstract class HudLabel : Label
    {
        protected HudLabel(Font font)
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
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

        protected abstract void Refresh();
    }
}
