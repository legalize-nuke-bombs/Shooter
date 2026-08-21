using System;
using Shooter.Game.Core.Saves;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class MainPage : MenuPage
    {
        private const string ContinueButton = "continue";
        private const string ContinueNote = "continue-note";
        private const string NewButton = "new";
        private const string LoadButton = "load";
        private const string JoinButton = "join";
        private const string QuitButton = "quit";

        private readonly Label note;
        private readonly Button resume;
        private SaveEntry latest;

        public MainPage(VisualElement root) : base(root)
        {
            resume = Require<Button>(ContinueButton);
            note = Require<Label>(ContinueNote);

            resume.clicked += () => Continuing?.Invoke(latest.Location);
            Require<Button>(NewButton).clicked += () => NewGameOpening?.Invoke();
            Require<Button>(LoadButton).clicked += () => SavesOpening?.Invoke();
            Require<Button>(JoinButton).clicked += () => JoinOpening?.Invoke();
            Require<Button>(QuitButton).clicked += () => Quitting?.Invoke();
        }

        public event Action<string> Continuing;

        public event Action NewGameOpening;

        public event Action SavesOpening;

        public event Action JoinOpening;

        public event Action Quitting;

        protected override void Opened()
        {
            latest = SaveLibrary.Latest();
            bool any = latest != null;

            resume.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            note.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;

            if (any) note.text = RussianDate.Moment(latest.Meta.Stamp);
        }
    }
}
