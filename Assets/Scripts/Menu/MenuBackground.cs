using UnityEngine;
using UnityEngine.UIElements;

public class MenuBackground : VisualElement
{
    private const float Cell = 48f;

    public MenuBackground()
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

        var painter = mgc.painter2D;
        int cols = Mathf.CeilToInt(rect.width / Cell);
        int rows = Mathf.CeilToInt(rect.height / Cell);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int hash = (x * 73856093) ^ (y * 19349663);
                hash = (hash >> 13) ^ hash;
                float variation = ((hash & 0xFF) / 255f - 0.5f) * 0.018f;

                painter.fillColor = new Color(0.075f + variation, 0.070f + variation, 0.062f + variation);
                painter.BeginPath();
                painter.MoveTo(new Vector2(x * Cell, y * Cell));
                painter.LineTo(new Vector2((x + 1) * Cell, y * Cell));
                painter.LineTo(new Vector2((x + 1) * Cell, (y + 1) * Cell));
                painter.LineTo(new Vector2(x * Cell, (y + 1) * Cell));
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
