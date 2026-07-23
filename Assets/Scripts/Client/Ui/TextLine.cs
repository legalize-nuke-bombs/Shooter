using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Ui
{
    public class TextLine : Label
    {
        public TextLine(Font font, int size)
        {
            pickingMode = PickingMode.Ignore;
            style.color = new Color(0.76f, 0.79f, 0.83f);
            style.fontSize = size;
            style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
            style.textShadow = new TextShadow
            {
                offset = Vector2.zero,
                blurRadius = 8f,
                color = new Color(0f, 0f, 0f, 0.9f)
            };
        }
    }
}
