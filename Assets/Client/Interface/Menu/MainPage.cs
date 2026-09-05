using System;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class MainPage : MenuPage
    {
        private const string NewButton = "new";
        private const string LoadButton = "load";
        private const string ServerSettingsButton = "server-settings";
        private const string ClientSettingsButton = "client-settings";
        private const string JoinButton = "join";
        private const string AccountButton = "account";
        private const string QuitButton = "quit";

        public MainPage(VisualElement root) : base(root)
        {
            Require<Button>(NewButton).clicked += () => NewGameOpening?.Invoke();
            Require<Button>(LoadButton).clicked += () => SavesOpening?.Invoke();
            Require<Button>(ServerSettingsButton).clicked += () => ServerSettingsOpening?.Invoke();
            Require<Button>(ClientSettingsButton).clicked += () => ClientSettingsOpening?.Invoke();
            Require<Button>(JoinButton).clicked += () => JoinOpening?.Invoke();
            Require<Button>(AccountButton).clicked += () => AccountOpening?.Invoke();
            Require<Button>(QuitButton).clicked += () => Quitting?.Invoke();
        }

        public event Action NewGameOpening;

        public event Action SavesOpening;

        public event Action ServerSettingsOpening;

        public event Action ClientSettingsOpening;

        public event Action JoinOpening;

        public event Action AccountOpening;

        public event Action Quitting;
    }
}
