using UnityEngine;
using UnityEngine.UIElements;

public class MenuBackground : VisualElement
{
    private const float Cell = 48f;
    private const float DriftSpeed = 5f;

    private static readonly float[] VignetteAlphas = { 0.16f, 0.115f, 0.08f, 0.05f, 0.025f };

    private float offset;

    public MenuBackground()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        generateVisualContent += OnGenerate;
        schedule.Execute(Drift).Every(33);
    }

    private void Drift(TimerState timer)
    {
        offset += DriftSpeed * timer.deltaTime / 1000f;
        MarkDirtyRepaint();
    }

    private void OnGenerate(MeshGenerationContext mgc)
    {
        Rect rect = mgc.visualElement.contentRect;
        if (rect.width <= 0f || rect.height <= 0f) return;

        var painter = mgc.painter2D;
        int baseCol = Mathf.FloorToInt(offset / Cell);
        float shift = offset - baseCol * Cell;

        int cols = Mathf.CeilToInt((rect.width + shift) / Cell);
        int rows = Mathf.CeilToInt(rect.height / Cell);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x <= cols; x++)
            {
                int hash = Hash(x + baseCol, y);
                float variation = ((hash & 0xFF) / 255f - 0.5f) * 0.025f;

                float cellX = x * Cell - shift;
                float cellY = y * Cell;
                painter.fillColor = new Color(0.052f + variation, 0.048f + variation, 0.042f + variation);
                FillRect(painter, new Rect(cellX, cellY, Cell - 1f, Cell - 1f));

                int detail = (hash >> 8) & 0xFF;
                if (detail < 46)
                {
                    int count = 2 + detail % 3;
                    for (int i = 0; i < count; i++)
                    {
                        int h = Hash(hash, i + 1);
                        float px = cellX + 3f + (h & 0xFFF) % (Cell - 13f);
                        float py = cellY + 3f + ((h >> 12) & 0xFFF) % (Cell - 13f);
                        float size = 4f + ((h >> 24) & 0x3);
                        painter.fillColor = new Color(0.033f, 0.030f, 0.026f);
                        FillRect(painter, new Rect(px, py, size, size));
                    }
                }
                else if (detail >= 250)
                {
                    int h = Hash(hash, 7);
                    float px = cellX + 6f + (h & 0xFFF) % (Cell - 16f);
                    float py = cellY + 6f + ((h >> 12) & 0xFFF) % (Cell - 16f);
                    painter.fillColor = new Color(0.13f, 0.19f, 0.23f);
                    FillRect(painter, new Rect(px, py, 4f, 4f));
                }
            }
        }

        DrawVignette(painter, rect);
    }

    private static void DrawVignette(Painter2D painter, Rect rect)
    {
        float band = Mathf.Min(rect.width, rect.height) * 0.035f;
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
