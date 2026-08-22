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
        private const string DangerClass = "dialog__title--danger";

        private readonly Button cancel;
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
            cancel = Require<Button>(document, CancelButton);

            confirm.clicked += Confirm;
            cancel.clicked += Cancel;
        }

        public bool Open { get; private set; }

        public void Ask(string question, string details, string confirmLabel, Action onConfirm)
        {
            Show(question, details, confirmLabel, onConfirm, false, false);
        }

        public void Warn(string question, string details, string confirmLabel, Action onConfirm)
        {
            Show(question, details, confirmLabel, onConfirm, true, false);
        }

        public void Notice(string question, string details, string confirmLabel)
        {
            Show(question, details, confirmLabel, null, true, true);
        }

        public void Cancel()
        {
            Close();
        }

        private void Show(string question, string details, string confirmLabel, Action onConfirm, bool danger, bool notice)
        {
            title.text = question;
            title.EnableInClassList(DangerClass, danger);
            text.text = details;
            confirm.text = confirmLabel;
            cancel.style.display = notice ? DisplayStyle.None : DisplayStyle.Flex;
            confirmed = onConfirm;

            Open = true;
            root.style.display = DisplayStyle.Flex;
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
