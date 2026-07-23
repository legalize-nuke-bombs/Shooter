using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Ui
{
    public abstract class UiElement : VisualElement
    {
        protected float Seconds { get; private set; }

        protected UiElement()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            generateVisualContent += Generate;
        }

        public void Tick(float dt)
        {
            Seconds += dt;
            OnTick(dt);
            foreach (VisualElement child in Children())
                if (child is UiElement element)
                    element.Tick(dt);
        }

        protected virtual void OnTick(float dt)
        {
        }

        protected virtual void Draw(Painter2D painter, Rect rect)
        {
        }

        protected bool Visible
        {
            set => style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected void Fullscreen()
        {
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
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

        private void Generate(MeshGenerationContext context)
        {
            Rect rect = context.visualElement.contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            Draw(context.painter2D, rect);
        }
    }
}
