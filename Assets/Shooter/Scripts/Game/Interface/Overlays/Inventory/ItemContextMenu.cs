using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public sealed class ItemContextMenu
    {
        private readonly VisualElement windowElement;
        private VisualElement curtainElement;

        public ItemContextMenu(VisualElement windowElement)
        {
            this.windowElement = windowElement;
        }

        public void Open(Vector2 position, IReadOnlyList<(string Text, Action Action)> items)
        {
            Close();
            if (items.Count == 0) return;

            curtainElement = new VisualElement();
            curtainElement.AddToClassList("menu-curtain");
            curtainElement.RegisterCallback<PointerDownEvent>(pointerDown =>
            {
                if (pointerDown.target == curtainElement) Close();
            });

            VisualElement menuElement = new();
            menuElement.AddToClassList("context-menu");

            Vector2 localPosition = windowElement.WorldToLocal(position);
            menuElement.style.left = localPosition.x;
            menuElement.style.top = localPosition.y;

            foreach ((string text, Action action) in items)
            {
                Button itemButton = new(() =>
                {
                    action();
                    Close();
                }) { text = text };
                itemButton.AddToClassList("context-menu__item");
                menuElement.Add(itemButton);
            }

            curtainElement.Add(menuElement);
            windowElement.Add(curtainElement);
        }

        public void Close()
        {
            if (curtainElement == null) return;

            curtainElement.RemoveFromHierarchy();
            curtainElement = null;
        }
    }
}
