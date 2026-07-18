using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Hud.Sleeping
{
    public class CalmDream : Dream
    {
        private const float BreathPeriod = 6f;
        private const float BaseAlpha = 0.88f;
        private const float BreathDepth = 0.05f;

        private static readonly Color VeilColor = new Color(0.008f, 0.012f, 0.024f);

        private float time;

        public override float Weight => 1f;

        public CalmDream()
        {
            generateVisualContent += OnGenerate;
            schedule.Execute(Tick).Every(33);
        }

        private void Tick(TimerState timer)
        {
            time += timer.deltaTime / 1000f;
            MarkDirtyRepaint();
        }

        private void OnGenerate(MeshGenerationContext mgc)
        {
            Rect rect = mgc.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            float breath = Mathf.Sin(time * 2f * Mathf.PI / BreathPeriod);
            float alpha = BaseAlpha + breath * BreathDepth;

            var painter = mgc.painter2D;
            painter.fillColor = new Color(VeilColor.r, VeilColor.g, VeilColor.b, alpha);
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, 0f));
            painter.LineTo(new Vector2(rect.width, 0f));
            painter.LineTo(new Vector2(rect.width, rect.height));
            painter.LineTo(new Vector2(0f, rect.height));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
