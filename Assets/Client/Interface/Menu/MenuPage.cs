using System;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public abstract class MenuPage
    {
        protected MenuPage(VisualElement root)
        {
            Root = root;
            Root.style.display = DisplayStyle.None;
        }

        public VisualElement Root { get; }

        internal void Show()
        {
            Root.style.display = DisplayStyle.Flex;
            Opened();
        }

        internal void Hide()
        {
            Root.style.display = DisplayStyle.None;
            Closed();
        }

        protected virtual void Opened()
        {
        }

        protected virtual void Closed()
        {
        }

        protected T Require<T>(string name) where T : VisualElement
        {
            T element = Root.Q<T>(name);
            if (element != null) return element;

            throw new InvalidOperationException($"Page {Root.name} has no {typeof(T).Name} named {name}");
        }
    }
}
