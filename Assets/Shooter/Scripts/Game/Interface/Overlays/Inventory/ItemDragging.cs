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
        private Vector2 ghostSize;
        private DraggedItem draggedItem;
        private int pointerId;

        public ItemDragging(VisualElement windowElement, Action<DraggedItem, Vector2> dropped)
        {
            this.windowElement = windowElement;
            this.dropped = dropped;

            windowElement.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            windowElement.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            windowElement.RegisterCallback<PointerCaptureOutEvent>(_ => Cancel());
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
                ghostSize = size;

                ghostElement = CreateGhost(iconSprite, size);
                windowElement.Add(ghostElement);
                MoveGhost(pointerDown.position);

                windowElement.CapturePointer(pointerId);
                pointerDown.StopPropagation();
            });
        }

        public void Cancel()
        {
            if (ghostElement == null) return;

            ghostElement.RemoveFromHierarchy();
            ghostElement = null;
            draggedItem = null;

            if (windowElement.HasPointerCapture(pointerId)) windowElement.ReleasePointer(pointerId);
        }

        private void HandlePointerMove(PointerMoveEvent pointerMove)
        {
            if (ghostElement == null || pointerMove.pointerId != pointerId) return;

            MoveGhost(pointerMove.position);
        }

        private void HandlePointerUp(PointerUpEvent pointerUp)
        {
            if (ghostElement == null || pointerUp.pointerId != pointerId) return;

            DraggedItem item = draggedItem;
            Cancel();
            dropped(item, pointerUp.position);
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

        private void MoveGhost(Vector2 position)
        {
            ghostElement.style.left = position.x - ghostSize.x / 2f;
            ghostElement.style.top = position.y - ghostSize.y / 2f;
        }
    }
}
