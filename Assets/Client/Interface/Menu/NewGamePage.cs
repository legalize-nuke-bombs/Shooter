using System;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class NewGamePage : ServerPage
    {
        private const string StartButton = "start";
        private const string BackButton = "back";

        public NewGamePage(VisualElement root) : base(root)
        {
            Require<Button>(StartButton).clicked += () => Starting?.Invoke();
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
        }

        public event Action Starting;

        public event Action Backing;
    }
}
