using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Menu
{
    public class MenuBackground : VisualElement
    {
        private const int GradientBands = 44;
        private const int DustCount = 70;
        private const int HazePatches = 7;

        private static readonly Color TopColor = new Color(0.020f, 0.024f, 0.016f);
        private static readonly Color MidColor = new Color(0.058f, 0.065f, 0.048f);
        private static readonly Color BottomColor = new Color(0.027f, 0.031f, 0.022f);
        private static readonly Color HazeColor = new Color(0.60f, 0.63f, 0.54f);
        private static readonly Color DustColor = new Color(0.79f, 0.78f, 0.73f);

        private static readonly float[] VignetteAlphas = { 0.30f, 0.22f, 0.15f, 0.09f, 0.045f };

        private float time;

        public MenuBackground()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
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
            DrawGradient(painter, rect);
            DrawHaze(painter, rect);
            DrawDust(painter, rect);
            DrawVignette(painter, rect);
        }

        private static void DrawGradient(Painter2D painter, Rect rect)
        {
            float bandHeight = rect.height / GradientBands;
            for (int i = 0; i < GradientBands; i++)
            {
                float t = (i + 0.5f) / GradientBands;
                painter.fillColor = t < 0.42f
                    ? Color.Lerp(TopColor, MidColor, t / 0.42f)
                    : Color.Lerp(MidColor, BottomColor, (t - 0.42f) / 0.58f);
                FillRect(painter, new Rect(0f, i * bandHeight, rect.width, bandHeight + 1f));
            }
        }

        private void DrawHaze(Painter2D painter, Rect rect)
        {
            for (int i = 0; i < HazePatches; i++)
            {
                int hash = Hash(i, 613);
                float width = rect.width * (0.25f + ((hash >> 4) & 0xFF) / 255f * 0.30f);
                float height = rect.height * (0.10f + ((hash >> 12) & 0xFF) / 255f * 0.10f);
                float y = rect.height * (((hash >> 20) & 0xFF) / 255f);
                float speed = 6f + (hash & 0xF);
                float x = Wrap(((hash >> 8) & 0xFFF) + time * speed, rect.width + width) - width;

                float alpha = 0.014f + ((hash >> 16) & 0x7) * 0.003f;
                for (int layer = 0; layer < 3; layer++)
                {
                    float grow = layer * 0.22f;
                    painter.fillColor = new Color(HazeColor.r, HazeColor.g, HazeColor.b, alpha * (1f - layer * 0.3f));
                    FillRect(painter, new Rect(x - width * grow * 0.5f, y - height * grow * 0.5f,
                        width * (1f + grow), height * (1f + grow)));
                }
            }
        }

        private void DrawDust(Painter2D painter, Rect rect)
        {
            for (int i = 0; i < DustCount; i++)
            {
                int hash = Hash(i, 27);
                float baseX = (hash & 0xFFF) / 4095f * rect.width;
                float baseY = ((hash >> 12) & 0xFFF) / 4095f * rect.height;
                float riseSpeed = 3f + ((hash >> 24) & 0x7) * 1.6f;
                float sway = 8f + ((hash >> 27) & 0x3) * 6f;

                float x = baseX + Mathf.Sin(time * 0.3f + i) * sway;
                float y = Wrap(baseY - time * riseSpeed, rect.height + 8f) - 4f;
                float size = 1f + ((hash >> 20) & 0x3);
                float alpha = 0.035f + ((hash >> 16) & 0xF) / 15f * 0.085f;

                painter.fillColor = new Color(DustColor.r, DustColor.g, DustColor.b, alpha);
                FillRect(painter, new Rect(x, y, size, size));
            }
        }

        private static void DrawVignette(Painter2D painter, Rect rect)
        {
            float band = Mathf.Min(rect.width, rect.height) * 0.05f;
            for (int i = 0; i < VignetteAlphas.Length; i++)
            {
                float inset = i * band;
                painter.fillColor = new Color(0f, 0f, 0f, VignetteAlphas[i]);
                FillRect(painter, new Rect(inset, inset, rect.width - inset * 2f, band));
                FillRect(painter, new Rect(inset, rect.height - inset - band, rect.width - inset * 2f, band));
                FillRect(painter, new Rect(inset, inset + band, band, rect.height - (inset + band) * 2f));
                FillRect(painter, new Rect(rect.width - inset - band, inset + band, band, rect.height - (inset + band) * 2f));
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
            if (r.width <= 0f || r.height <= 0f) return;
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
