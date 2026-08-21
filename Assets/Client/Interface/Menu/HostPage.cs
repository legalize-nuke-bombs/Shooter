using System;
using System.Collections.Generic;
using Shooter.Configuring;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class HostPage : MenuPage
    {
        private const string SavesElement = "saves";
        private const string EmptyElement = "empty";
        private const string NewButton = "new";
        private const string BackButton = "back";
        private static readonly Journal Log = Logs.Here();

        private readonly Label empty;
        private readonly ListView saves;
        private List<SaveEntry> entries = new();

        public HostPage(VisualElement root) : base(root)
        {
            saves = Require<ListView>(SavesElement);
            empty = Require<Label>(EmptyElement);

            saves.makeItem = () => new SaveCard();
            saves.bindItem = (element, index) => ((SaveCard)element).Show(entries[index], Load, Delete);
            saves.unbindItem = (element, index) => ((SaveCard)element).Release();
            saves.destroyItem = element => ((SaveCard)element).Release();

            Require<Button>(NewButton).clicked += () => Starting?.Invoke();
            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
        }

        public event Action<string> Loading;

        public event Action Starting;

        public event Action Backing;

        public override bool Wide => true;

        protected override void Opened()
        {
            Refresh();
        }

        protected override void Closed()
        {
            Config.Save();

            GameConfig config = Config.Read();
            Log.Info($"Own world on port {config.Server.Port} under the name {config.Client.Name}");
        }

        private void Refresh()
        {
            entries = SaveLibrary.All();
            saves.itemsSource = entries;
            saves.RefreshItems();

            bool any = entries.Count > 0;
            saves.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            empty.style.display = any ? DisplayStyle.None : DisplayStyle.Flex;

            Log.Info($"Host page lists {entries.Count} saves");
        }

        private void Load(SaveEntry entry)
        {
            Loading?.Invoke(entry.Location);
        }

        private void Delete(SaveEntry entry)
        {
            SaveLibrary.Delete(entry);
            Refresh();
        }
    }
}
