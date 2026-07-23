using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;

namespace Shooter.Client.Hud
{
    public class Crosshair : UiElement
    {
        private const float ArmLength = 5f;
        private const float ArmGap = 3f;
        private const float ArmThickness = 1.5f;

        private static readonly Color ArmColor = new Color(0.85f, 0.89f, 0.96f, 0.7f);

        public Crosshair()
        {
            Fullscreen();
        }

        protected override void Draw(Painter2D painter, Rect rect)
        {
            var center = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            painter.strokeColor = ArmColor;
            painter.lineWidth = ArmThickness;
            DrawArm(painter, center, Vector2.left);
            DrawArm(painter, center, Vector2.right);
            DrawArm(painter, center, Vector2.up);
            DrawArm(painter, center, Vector2.down);
        }

        private static void DrawArm(Painter2D painter, Vector2 center, Vector2 direction)
        {
            painter.BeginPath();
            painter.MoveTo(center + direction * ArmGap);
            painter.LineTo(center + direction * (ArmGap + ArmLength));
            painter.Stroke();
        }
    }
}
