using System;
using System.Collections.Generic;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class SavesPage : MenuPage
    {
        private const string SavesElement = "saves";
        private const string EmptyElement = "empty";
        private const string BackButton = "back";
        private const string DeleteQuestion = "Удалить сохранение?";
        private const string DeleteLabel = "Удалить";
        private const string ForeignQuestion = "Другая версия";
        private const string ForeignLabel = "Загрузить";
        private static readonly Journal Log = Logs.Here();

        private readonly Dialog dialog;
        private readonly Label empty;
        private readonly ListView saves;
        private List<SaveEntry> entries = new();

        public SavesPage(VisualElement root, Dialog dialog) : base(root)
        {
            this.dialog = dialog;
            saves = Require<ListView>(SavesElement);
            empty = Require<Label>(EmptyElement);

            saves.makeItem = () => new SaveCard();
            saves.bindItem = (element, index) => ((SaveCard)element).Show(entries[index], AskToLoad, AskToDelete);
            saves.unbindItem = (element, index) => ((SaveCard)element).Release();
            saves.destroyItem = element => ((SaveCard)element).Release();

            Require<Button>(BackButton).clicked += () => Backing?.Invoke();
        }

        public event Action<string> Loading;

        public event Action Backing;

        protected override void Opened()
        {
            Refresh();
        }

        private void Refresh()
        {
            entries = SaveLibrary.All();
            saves.itemsSource = entries;
            saves.RefreshItems();

            bool any = entries.Count > 0;
            saves.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            empty.style.display = any ? DisplayStyle.None : DisplayStyle.Flex;

            Log.Info($"Saves page lists {entries.Count} saves");
        }

        private void AskToLoad(SaveEntry entry)
        {
            if (!entry.Foreign)
            {
                Load(entry);
                return;
            }

            dialog.Warn(ForeignQuestion,
                $"Сохранение {entry.Meta.Version}, игра {Application.version}. Совместимость не гарантируется.",
                ForeignLabel, () => Load(entry));
        }

        private void Load(SaveEntry entry)
        {
            Loading?.Invoke(entry.Location);
        }

        private void AskToDelete(SaveEntry entry)
        {
            dialog.Warn(DeleteQuestion, RussianDate.Moment(entry.Meta.Stamp), DeleteLabel, () => Delete(entry));
        }

        private void Delete(SaveEntry entry)
        {
            SaveLibrary.Delete(entry);
            Refresh();
        }
    }
}
