using System;
using UnityEngine.UIElements;

namespace Shooter.Client.Menu
{
    public class ServerErrorScreen
    {
        private readonly VisualElement screen;
        private readonly Label detail;

        public ServerErrorScreen(VisualElement root, Action onRetry)
        {
            screen = root.Q<VisualElement>("server-error-screen");
            detail = root.Q<Label>("server-error-detail");
            root.Q<Button>("retry-btn").clicked += onRetry;
        }

        public void Show(string detailText)
        {
            detail.text = detailText;
            screen.RemoveFromClassList("hidden");
        }

        public void Hide() => screen.AddToClassList("hidden");
    }
}
