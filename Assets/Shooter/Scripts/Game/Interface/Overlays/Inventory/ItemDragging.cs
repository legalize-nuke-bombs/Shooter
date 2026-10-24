using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public sealed class ItemDragging
    {
        private readonly VisualElement windowElement;
        private readonly Action<DraggedItem, Vector2> dropped;
        private VisualElement ghostElement;
        private DraggedItem draggedItem;
        private int pointerId;

        public ItemDragging(VisualElement windowElement, Action<DraggedItem, Vector2> dropped)
        {
            this.windowElement = windowElement;
            this.dropped = dropped;
        }

        public bool IsDragging => ghostElement != null;

        public void MakeDraggable(VisualElement itemElement, ItemAsset asset, Vector2 size, DraggedItem item)
        {
            Sprite iconSprite = ItemGrid.GetIconSprite(asset);

            itemElement.RegisterCallback<PointerDownEvent>(pointerDown =>
            {
                if (pointerDown.button != 0 || ghostElement != null) return;

                draggedItem = item;
                pointerId = pointerDown.pointerId;

                ghostElement = CreateGhost(iconSprite, size);
                windowElement.Add(ghostElement);
                MoveGhost(pointerDown.position, size);

                itemElement.CapturePointer(pointerId);
                pointerDown.StopPropagation();
            });

            itemElement.RegisterCallback<PointerMoveEvent>(pointerMove =>
            {
                if (ghostElement == null || pointerMove.pointerId != pointerId) return;

                MoveGhost(pointerMove.position, size);
            });

            itemElement.RegisterCallback<PointerUpEvent>(pointerUp =>
            {
                if (ghostElement == null || pointerUp.pointerId != pointerId) return;

                itemElement.ReleasePointer(pointerId);
                ghostElement.RemoveFromHierarchy();
                ghostElement = null;
                dropped(draggedItem, pointerUp.position);
            });
        }

        private static VisualElement CreateGhost(Sprite iconSprite, Vector2 size)
        {
            VisualElement ghost = new();
            ghost.AddToClassList("ghost");
            ghost.style.width = size.x;
            ghost.style.height = size.y;

            if (iconSprite != null) ghost.style.backgroundImage = Background.FromSprite(iconSprite);

            return ghost;
        }

        private void MoveGhost(Vector2 position, Vector2 size)
        {
            ghostElement.style.left = position.x - size.x / 2f;
            ghostElement.style.top = position.y - size.y / 2f;
        }
    }
}
