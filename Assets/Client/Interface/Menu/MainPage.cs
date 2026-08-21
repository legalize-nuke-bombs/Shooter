using System;
using Shooter.Game.Core.Saves;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class MainPage : MenuPage
    {
        private const string ContinueButton = "continue";
        private const string ContinueNote = "continue-note";
        private const string WorldsButton = "worlds";
        private const string NewButton = "new";
        private const string JoinButton = "join";
        private const string SettingsButton = "settings";
        private const string QuitButton = "quit";

        private readonly Label note;
        private readonly Button resume;
        private readonly Button worlds;
        private SaveEntry latest;

        public MainPage(VisualElement root) : base(root)
        {
            resume = Require<Button>(ContinueButton);
            note = Require<Label>(ContinueNote);
            worlds = Require<Button>(WorldsButton);

            resume.clicked += () => Continuing?.Invoke(latest.Location);
            worlds.clicked += () => Browsing?.Invoke();
            Require<Button>(NewButton).clicked += () => Starting?.Invoke();
            Require<Button>(JoinButton).clicked += () => Joining?.Invoke();
            Require<Button>(SettingsButton).clicked += () => Configuring?.Invoke();
            Require<Button>(QuitButton).clicked += () => Quitting?.Invoke();
        }

        public event Action<string> Continuing;

        public event Action Browsing;

        public event Action Starting;

        public event Action Joining;

        public event Action Configuring;

        public event Action Quitting;

        protected override void Opened()
        {
            latest = SaveLibrary.Latest();
            bool any = latest != null;

            resume.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            note.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            worlds.SetEnabled(any);

            if (any) note.text = RussianDate.Moment(latest.Meta.Stamp);
        }
    }
}
