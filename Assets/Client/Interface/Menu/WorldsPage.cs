using System;
using System.Collections.Generic;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class WorldsPage : MenuPage
    {
        private const string ListElement = "list";
        private const string EmptyElement = "empty";
        private const string BackButton = "back";
        private static readonly Journal Log = Logs.Here();

        private readonly Label empty;
        private readonly ListView list;
        private List<SaveEntry> entries = new();

        public WorldsPage(VisualElement root) : base(root)
        {
            list = Require<ListView>(ListElement);
            empty = Require<Label>(EmptyElement);

            list.makeItem = () => new WorldCard();
            list.bindItem = (element, index) => ((WorldCard)element).Show(entries[index], Load, Delete);
            list.unbindItem = (element, index) => ((WorldCard)element).Release();
            list.destroyItem = element => ((WorldCard)element).Release();

            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
        }

        public event Action<string> Loading;

        public event Action Backing;

        public override bool Wide => true;

        protected override void Opened()
        {
            Refresh();
        }

        private void Refresh()
        {
            entries = SaveLibrary.All();
            list.itemsSource = entries;
            list.RefreshItems();

            bool any = entries.Count > 0;
            list.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            empty.style.display = any ? DisplayStyle.None : DisplayStyle.Flex;

            Log.Info($"Worlds page lists {entries.Count} saves");
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
