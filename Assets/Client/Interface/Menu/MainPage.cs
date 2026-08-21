using System;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class MainPage : MenuPage
    {
        private const string NewButton = "new";
        private const string LoadButton = "load";
        private const string JoinButton = "join";
        private const string QuitButton = "quit";

        public MainPage(VisualElement root) : base(root)
        {
            Require<Button>(NewButton).clicked += () => NewGameOpening?.Invoke();
            Require<Button>(LoadButton).clicked += () => SavesOpening?.Invoke();
            Require<Button>(JoinButton).clicked += () => JoinOpening?.Invoke();
            Require<Button>(QuitButton).clicked += () => Quitting?.Invoke();
        }

        public event Action NewGameOpening;

        public event Action SavesOpening;

        public event Action JoinOpening;

        public event Action Quitting;
    }
}
