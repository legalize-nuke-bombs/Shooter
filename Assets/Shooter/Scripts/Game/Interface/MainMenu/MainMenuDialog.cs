using System;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class MainMenuDialog
    {
        private const string DialogElementName = "dialog";
        private const string TitleLabelName = "dialog-title";
        private const string TextLabelName = "dialog-text";
        private const string ConfirmButtonName = "dialog-confirm";
        private const string CancelButtonName = "dialog-cancel";
        private const string WindowElementName = "dialog-window";
        private const string DangerClass = "dialog__title--danger";
        private const string BusyClass = "dialog__window--busy";

        private readonly Button cancelButton;
        private readonly Button confirmButton;
        private readonly VisualElement dialogElement;
        private readonly VisualElement windowElement;
        private readonly Label textLabel;
        private readonly Label titleLabel;
        private Action confirmCallback;
        private bool busy;

        public MainMenuDialog(VisualElement document)
        {
            dialogElement = RequireElement<VisualElement>(document, DialogElementName);
            windowElement = RequireElement<VisualElement>(document, WindowElementName);
            titleLabel = RequireElement<Label>(document, TitleLabelName);
            textLabel = RequireElement<Label>(document, TextLabelName);
            confirmButton = RequireElement<Button>(document, ConfirmButtonName);
            cancelButton = RequireElement<Button>(document, CancelButtonName);

            confirmButton.clicked += HandleConfirmClicked;
            cancelButton.clicked += CloseUnlessBusy;
        }

        public bool IsOpen { get; private set; }

        public void ShowQuestion(string title, string details, string confirmLabel, Action confirmCallback)
        {
            ShowDialog(title, details, confirmLabel, confirmCallback, false, false);
        }

        public void ShowDangerousQuestion(string title, string details, string confirmLabel, Action confirmCallback)
        {
            ShowDialog(title, details, confirmLabel, confirmCallback, true, false);
        }

        public void ShowNotice(string title, string details, string confirmLabel)
        {
            ShowDialog(title, details, confirmLabel, null, true, true);
        }

        public void ShowBusy(string title, string details)
        {
            windowElement.AddToClassList(BusyClass);
            titleLabel.text = title;
            titleLabel.EnableInClassList(DangerClass, false);
            textLabel.text = details;
            textLabel.style.display = DisplayStyle.Flex;
            confirmButton.style.display = DisplayStyle.None;
            cancelButton.style.display = DisplayStyle.None;
            confirmCallback = null;

            busy = true;
            IsOpen = true;
            dialogElement.style.display = DisplayStyle.Flex;
        }

        public void CloseBusy()
        {
            busy = false;
            CloseDialog();
        }

        public void CloseUnlessBusy()
        {
            if (busy) return;

            CloseDialog();
        }

        private void ShowDialog(string title, string details, string confirmLabel, Action confirmCallback, bool dangerous, bool notice)
        {
            windowElement.RemoveFromClassList(BusyClass);
            busy = false;
            titleLabel.text = title;
            titleLabel.EnableInClassList(DangerClass, dangerous);
            textLabel.text = details;
            textLabel.style.display = DisplayStyle.Flex;
            confirmButton.text = confirmLabel;
            confirmButton.style.display = DisplayStyle.Flex;
            cancelButton.style.display = notice ? DisplayStyle.None : DisplayStyle.Flex;
            this.confirmCallback = confirmCallback;

            IsOpen = true;
            dialogElement.style.display = DisplayStyle.Flex;
        }

        private void HandleConfirmClicked()
        {
            Action callback = confirmCallback;
            CloseDialog();
            callback?.Invoke();
        }

        private void CloseDialog()
        {
            confirmCallback = null;
            IsOpen = false;
            dialogElement.style.display = DisplayStyle.None;
        }

        private static T RequireElement<T>(VisualElement document, string elementName) where T : VisualElement
        {
            T element = document.Q<T>(elementName);
            if (element != null) return element;

            throw new InvalidOperationException($"Menu document has no {typeof(T).Name} named {elementName}");
        }
    }
}
