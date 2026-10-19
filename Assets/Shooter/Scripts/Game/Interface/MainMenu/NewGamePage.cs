using System;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class NewGamePage : ServerPage
    {
        private const string StartButtonName = "start";
        private const string BackButtonName = "back";

        public NewGamePage(VisualElement root) : base(root)
        {
            RequireElement<Button>(StartButtonName).clicked += () => StartClicked?.Invoke();
            RequireElement<Button>(BackButtonName).clicked += () => BackClicked?.Invoke();
        }

        public event Action StartClicked;

        public event Action BackClicked;
    }
}
