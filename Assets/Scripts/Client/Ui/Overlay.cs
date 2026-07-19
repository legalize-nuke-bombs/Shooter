using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Ui
{
    public abstract class Overlay : VisualElement
    {
        protected float Seconds { get; private set; }

        protected Overlay()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            generateVisualContent += Generate;
        }

        protected virtual void Draw(Painter2D painter, Rect rect)
        {
        }

        protected void Animate(long intervalMs)
        {
            schedule.Execute(Advance).Every(intervalMs);
        }

        protected static void FillRect(Painter2D painter, Rect rect)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.x, rect.y));
            painter.LineTo(new Vector2(rect.xMax, rect.y));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.x, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }

        private void Advance(TimerState timer)
        {
            Seconds += timer.deltaTime / 1000f;
            MarkDirtyRepaint();
        }

        private void Generate(MeshGenerationContext context)
        {
            Rect rect = context.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            Draw(context.painter2D, rect);
        }
    }
}
