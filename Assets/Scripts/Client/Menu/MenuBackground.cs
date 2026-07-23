using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Ui;

namespace Shooter.Client.Menu
{
    public class MenuBackground : UiElement
    {
        private const int GradientBands = 44;
        private const int StarCount = 90;
        private const int HazePatches = 5;
        private const float SkyDriftSpeed = 1.6f;

        private static readonly Color ZenithColor = new Color(0.014f, 0.018f, 0.036f);
        private static readonly Color MidSkyColor = new Color(0.036f, 0.050f, 0.086f);
        private static readonly Color HorizonColor = new Color(0.062f, 0.088f, 0.140f);
        private static readonly Color HazeColor = new Color(0.54f, 0.61f, 0.72f);
        private static readonly Color StarColor = new Color(0.85f, 0.89f, 0.96f);

        private static readonly float[] VignetteAlphas = { 0.26f, 0.19f, 0.13f, 0.08f, 0.04f };

        public MenuBackground()
        {
            Fullscreen();
        }

        protected override void OnTick(float dt)
        {
            MarkDirtyRepaint();
        }

        protected override void Draw(Painter2D painter, Rect rect)
        {
            DrawSky(painter, rect);
            DrawHaze(painter, rect);
            DrawStars(painter, rect);
            DrawVignette(painter, rect);
        }

        private static void DrawSky(Painter2D painter, Rect rect)
        {
            float bandHeight = rect.height / GradientBands;
            for (int i = 0; i < GradientBands; i++)
            {
                float t = (i + 0.5f) / GradientBands;
                painter.fillColor = t < 0.60f
                    ? Color.Lerp(ZenithColor, MidSkyColor, t / 0.60f)
                    : Color.Lerp(MidSkyColor, HorizonColor, (t - 0.60f) / 0.40f);
                FillRect(painter, new Rect(0f, i * bandHeight, rect.width, bandHeight + 1f));
            }
        }

        private void DrawHaze(Painter2D painter, Rect rect)
        {
            for (int i = 0; i < HazePatches; i++)
            {
                int hash = Noise.Hash(i, 613);
                float width = rect.width * (0.25f + ((hash >> 4) & 0xFF) / 255f * 0.30f);
                float height = rect.height * (0.08f + ((hash >> 12) & 0xFF) / 255f * 0.08f);
                float y = rect.height * (((hash >> 20) & 0xFF) / 255f);
                float speed = 3f + (hash & 0x7);
                float x = Noise.Wrap(((hash >> 8) & 0xFFF) + Seconds * speed, rect.width + width) - width;

                float alpha = 0.012f + ((hash >> 16) & 0x7) * 0.0025f;
                for (int layer = 0; layer < 3; layer++)
                {
                    float grow = layer * 0.22f;
                    painter.fillColor = new Color(HazeColor.r, HazeColor.g, HazeColor.b, alpha * (1f - layer * 0.3f));
                    FillRect(painter, new Rect(x - width * grow * 0.5f, y - height * grow * 0.5f,
                        width * (1f + grow), height * (1f + grow)));
                }
            }
        }

        private void DrawStars(Painter2D painter, Rect rect)
        {
            for (int i = 0; i < StarCount; i++)
            {
                int hash = Noise.Hash(i, 27);
                float baseX = (hash & 0xFFF) / 4095f * rect.width;
                float yNorm = ((hash >> 12) & 0xFFF) / 4095f;
                float y = yNorm * yNorm * rect.height;

                float x = Noise.Wrap(baseX + Seconds * SkyDriftSpeed, rect.width + 4f) - 2f;

                float twinkleSpeed = 0.4f + ((hash >> 24) & 0x7) * 0.25f;
                float phase = ((hash >> 8) & 0xFF) / 255f * 6.2832f;
                float twinkle = 0.62f + 0.38f * Mathf.Sin(Seconds * twinkleSpeed + phase);

                bool bright = (hash & 0x3F) < 5;
                float size = bright ? 2.6f : 1f + ((hash >> 20) & 0x1);
                float alpha = (bright ? 0.30f : 0.05f + ((hash >> 16) & 0xF) / 15f * 0.12f) * twinkle;

                painter.fillColor = new Color(StarColor.r, StarColor.g, StarColor.b, alpha);
                FillRect(painter, new Rect(x - size * 0.5f, y - size * 0.5f, size, size));

                if (bright)
                {
                    painter.fillColor = new Color(StarColor.r, StarColor.g, StarColor.b, alpha * 0.35f);
                    FillRect(painter, new Rect(x - size * 1.6f, y - 0.5f, size * 3.2f, 1f));
                    FillRect(painter, new Rect(x - 0.5f, y - size * 1.6f, 1f, size * 3.2f));
                }
            }
        }

        private static void DrawVignette(Painter2D painter, Rect rect)
        {
            float band = Mathf.Min(rect.width, rect.height) * 0.05f;
            for (int i = 0; i < VignetteAlphas.Length; i++)
            {
                float inset = i * band;
                painter.fillColor = new Color(0f, 0f, 0.005f, VignetteAlphas[i]);
                FillRect(painter, new Rect(inset, inset, rect.width - inset * 2f, band));
                FillRect(painter, new Rect(inset, rect.height - inset - band, rect.width - inset * 2f, band));
                FillRect(painter, new Rect(inset, inset + band, band, rect.height - (inset + band) * 2f));
                FillRect(painter, new Rect(rect.width - inset - band, inset + band, band, rect.height - (inset + band) * 2f));
            }
        }

    }
}
