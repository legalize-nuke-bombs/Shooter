using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Hud.Sleeping
{
    public class CalmDream : Dream
    {
        private const float BreathPeriod = 6f;
        private const float BaseAlpha = 0.97f;
        private const float BreathDepth = 0.02f;
        private const int StarCount = 10;
        private const float StarDrift = 4f;

        private static readonly Color VeilColor = new Color(0.008f, 0.012f, 0.024f);
        private static readonly Color StarColor = new Color(0.85f, 0.89f, 0.96f);

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

            var painter = mgc.painter2D;

            float breath = Mathf.Sin(time * 2f * Mathf.PI / BreathPeriod);
            painter.fillColor = new Color(VeilColor.r, VeilColor.g, VeilColor.b, BaseAlpha + breath * BreathDepth);
            FillRect(painter, new Rect(0f, 0f, rect.width, rect.height));

            for (int i = 0; i < StarCount; i++)
            {
                int hash = Hash(i, 101);
                float x = Wrap((hash & 0xFFF) / 4095f * rect.width + time * StarDrift, rect.width);
                float y = ((hash >> 12) & 0xFFF) / 4095f * rect.height;
                float twinkle = 0.5f + 0.5f * Mathf.Sin(time * 0.6f + ((hash >> 8) & 0xFF));
                float size = 1.5f + ((hash >> 20) & 0x1);

                painter.fillColor = new Color(StarColor.r, StarColor.g, StarColor.b, 0.12f + 0.25f * twinkle);
                FillRect(painter, new Rect(x - size * 0.5f, y - size * 0.5f, size, size));
            }
        }

        private static float Wrap(float value, float range)
        {
            float wrapped = value % range;
            return wrapped < 0f ? wrapped + range : wrapped;
        }

        private static int Hash(int a, int b)
        {
            int hash = (a * 73856093) ^ (b * 19349663);
            return (hash >> 13) ^ hash;
        }

        private static void FillRect(Painter2D painter, Rect r)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(r.x, r.y));
            painter.LineTo(new Vector2(r.xMax, r.y));
            painter.LineTo(new Vector2(r.xMax, r.yMax));
            painter.LineTo(new Vector2(r.x, r.yMax));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
