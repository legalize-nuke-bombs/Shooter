using System;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class ServerSettingsPage : ServerPage
    {
        private const string BackButton = "back";

        public ServerSettingsPage(VisualElement root) : base(root)
        {
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
        }

        public event Action Backing;
    }
}
