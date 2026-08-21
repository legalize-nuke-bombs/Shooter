using System;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class MainPage : MenuPage
    {
        private const string HostButton = "host";
        private const string ClientButton = "client";
        private const string QuitButton = "quit";

        public MainPage(VisualElement root) : base(root)
        {
            Require<Button>(HostButton).clicked += () => HostOpening?.Invoke();
            Require<Button>(ClientButton).clicked += () => ClientOpening?.Invoke();
            Require<Button>(QuitButton).clicked += () => Quitting?.Invoke();
        }

        public event Action HostOpening;

        public event Action ClientOpening;

        public event Action Quitting;
    }
}
