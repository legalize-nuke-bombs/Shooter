using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public abstract class MainMenuPage
    {
        protected MainMenuPage(VisualElement root)
        {
            Root = root;
            Root.style.display = DisplayStyle.None;
        }

        public VisualElement Root { get; }

        internal void ShowPage()
        {
            Root.style.display = DisplayStyle.Flex;
            HandlePageShown();
        }

        internal void HidePage()
        {
            Root.style.display = DisplayStyle.None;
            HandlePageHidden();
        }

        protected virtual void HandlePageShown()
        {
        }

        protected virtual void HandlePageHidden()
        {
        }

        protected static void SetDropdownChoices(DropdownField dropdownField, IEnumerable<string> keys, Func<string, string> getTitle)
        {
            dropdownField.choices = keys.ToList();
            dropdownField.formatSelectedValueCallback = getTitle;
            dropdownField.formatListItemCallback = getTitle;
        }

        protected T RequireElement<T>(string elementName) where T : VisualElement
        {
            T element = Root.Q<T>(elementName);
            if (element != null) return element;

            throw new InvalidOperationException($"Page {Root.name} has no {typeof(T).Name} named {elementName}");
        }
    }
}
