using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;

namespace Shooter.Client.Hud.Sleeping
{
    public class CalmDream : Dream
    {
        private const long TickMs = 33;
        private const int StarCount = 10;
        private const float StarDrift = 4f;

        private static readonly Color VeilColor = new Color(0.008f, 0.012f, 0.024f);
        private static readonly Color StarColor = new Color(0.85f, 0.89f, 0.96f);

        public override float Weight => 1f;

        public CalmDream()
        {
            Animate(TickMs);
        }

        protected override void Draw(Painter2D painter, Rect rect)
        {
            painter.fillColor = VeilColor;
            FillRect(painter, new Rect(0f, 0f, rect.width, rect.height));

            for (int i = 0; i < StarCount; i++)
            {
                int hash = Noise.Hash(i, 101);
                float x = Noise.Wrap((hash & 0xFFF) / 4095f * rect.width + Seconds * StarDrift, rect.width);
                float y = ((hash >> 12) & 0xFFF) / 4095f * rect.height;
                float twinkle = 0.5f + 0.5f * Mathf.Sin(Seconds * 0.6f + ((hash >> 8) & 0xFF));
                float size = 1.5f + ((hash >> 20) & 0x1);

                painter.fillColor = new Color(StarColor.r, StarColor.g, StarColor.b, 0.12f + 0.25f * twinkle);
                FillRect(painter, new Rect(x - size * 0.5f, y - size * 0.5f, size, size));
            }
        }
    }
}
