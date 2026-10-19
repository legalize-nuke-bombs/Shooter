using System;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class MainPage : MainMenuPage
    {
        private const string NewGameButtonName = "new";
        private const string LoadGameButtonName = "load";
        private const string ServerSettingsButtonName = "server-settings";
        private const string ClientSettingsButtonName = "client-settings";
        private const string JoinButtonName = "join";
        private const string AccountButtonName = "account";
        private const string QuitButtonName = "quit";

        public MainPage(VisualElement root) : base(root)
        {
            RequireElement<Button>(NewGameButtonName).clicked += () => NewGameClicked?.Invoke();
            RequireElement<Button>(LoadGameButtonName).clicked += () => LoadGameClicked?.Invoke();
            RequireElement<Button>(ServerSettingsButtonName).clicked += () => ServerSettingsClicked?.Invoke();
            RequireElement<Button>(ClientSettingsButtonName).clicked += () => ClientSettingsClicked?.Invoke();
            RequireElement<Button>(JoinButtonName).clicked += () => JoinClicked?.Invoke();
            RequireElement<Button>(AccountButtonName).clicked += () => AccountClicked?.Invoke();
            RequireElement<Button>(QuitButtonName).clicked += () => QuitClicked?.Invoke();
        }

        public event Action NewGameClicked;

        public event Action LoadGameClicked;

        public event Action ServerSettingsClicked;

        public event Action ClientSettingsClicked;

        public event Action JoinClicked;

        public event Action AccountClicked;

        public event Action QuitClicked;
    }
}
