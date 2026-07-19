using UnityEngine.UIElements;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities.Players;
using UnityEngine;

namespace Shooter.Client.Hud
{
    public class HpBar : VisualElement
    {

        private static readonly Color Color = new Color(0, 0, 0);
        private static readonly Color FillColor = new Color(0.5f, 0, 0);
        private const float Thickness = 15;
        private const float RelOffsetX = 0.2f;
        private const float RelOffsetY = 0.80f;
        private const float RelWidth = 0.2f;

        public HpBar()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            generateVisualContent += OnGenerate;
        }

        private static void OnGenerate(MeshGenerationContext mgc)
        {
            Rect rect = mgc.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            ClientWorld world = NetworkClient.Instance?.World;
            if (world?.Players == null) return;

            PlayerState playerState = world.Players[world.PlayerId];
            float fraction = playerState.Hp / (float)playerState.MaxHp;

            var painter = mgc.painter2D;

            painter.strokeColor = Color;
            painter.lineWidth = Thickness;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.width * RelOffsetX, rect.height * RelOffsetY));
            painter.LineTo(new Vector2(rect.width * (RelOffsetX + RelWidth), rect.height * RelOffsetY));
            painter.Stroke();

            painter.strokeColor = FillColor;
            painter.lineWidth = Thickness;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.width * RelOffsetX, rect.height * RelOffsetY));
            painter.LineTo(new Vector2(rect.width * (RelOffsetX + RelWidth * fraction), rect.height * RelOffsetY));
            painter.Stroke();
        }
    }
}
