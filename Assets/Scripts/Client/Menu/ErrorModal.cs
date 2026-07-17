using UnityEngine.UIElements;

namespace Shooter.Client.Menu
{
    public class ErrorModal
    {
        private readonly VisualElement modal;
        private readonly Label text;

        public ErrorModal(VisualElement root)
        {
            modal = root.Q<VisualElement>("error-modal");
            text = root.Q<Label>("error-modal-text");
            root.Q<Button>("error-modal-ok").clicked += Hide;
        }

        public void Show(string message)
        {
            text.text = message;
            modal.RemoveFromClassList("hidden");
        }

        public void Hide() => modal.AddToClassList("hidden");
    }
}
