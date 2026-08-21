using System;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class Dialog
    {
        private const string RootElement = "dialog";
        private const string TitleElement = "dialog-title";
        private const string TextElement = "dialog-text";
        private const string ConfirmButton = "dialog-confirm";
        private const string CancelButton = "dialog-cancel";

        private readonly Button confirm;
        private readonly VisualElement root;
        private readonly Label text;
        private readonly Label title;
        private Action confirmed;

        public Dialog(VisualElement document)
        {
            root = Require<VisualElement>(document, RootElement);
            title = Require<Label>(document, TitleElement);
            text = Require<Label>(document, TextElement);
            confirm = Require<Button>(document, ConfirmButton);

            confirm.clicked += Confirm;
            Require<Button>(document, CancelButton).clicked += Cancel;
        }

        public bool Open { get; private set; }

        public void Ask(string question, string details, string confirmLabel, Action onConfirm)
        {
            title.text = question;
            text.text = details;
            confirm.text = confirmLabel;
            confirmed = onConfirm;

            Open = true;
            root.style.display = DisplayStyle.Flex;
        }

        public void Cancel()
        {
            Close();
        }

        private void Confirm()
        {
            Action action = confirmed;
            Close();
            action?.Invoke();
        }

        private void Close()
        {
            confirmed = null;
            Open = false;
            root.style.display = DisplayStyle.None;
        }

        private static T Require<T>(VisualElement document, string name) where T : VisualElement
        {
            T element = document.Q<T>(name);
            if (element != null) return element;

            throw new InvalidOperationException($"Menu document has no {typeof(T).Name} named {name}");
        }
    }
}
