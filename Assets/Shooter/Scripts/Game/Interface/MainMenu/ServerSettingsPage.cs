using System;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ServerSettingsPage : ServerPage
    {
        private const string BackButtonName = "back";

        public ServerSettingsPage(VisualElement root) : base(root)
        {
            RequireElement<Button>(BackButtonName).clicked += () => BackClicked?.Invoke();
        }

        public event Action BackClicked;
    }
}
