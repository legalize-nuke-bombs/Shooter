using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class SavesPage : MainMenuPage
    {
        private const string SaveListViewName = "saves";
        private const string EmptyLabelName = "empty";
        private const string BackButtonName = "back";
        private const string DeleteQuestion = "Удалить сохранение?";
        private const string DeleteConfirmLabel = "Удалить";
        private const string ForeignVersionQuestion = "Другая версия";
        private const string ForeignVersionConfirmLabel = "Загрузить";
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly MainMenuDialog dialog;
        private readonly Label emptyLabel;
        private readonly ListView saveListView;
        private List<SaveEntry> saveEntries = new();

        public SavesPage(VisualElement root, MainMenuDialog dialog) : base(root)
        {
            this.dialog = dialog;
            saveListView = RequireElement<ListView>(SaveListViewName);
            emptyLabel = RequireElement<Label>(EmptyLabelName);

            saveListView.makeItem = () => new SaveCard();
            saveListView.bindItem = (element, index) => ((SaveCard)element).ShowSaveEntry(saveEntries[index], AskToLoad, AskToDelete);
            saveListView.unbindItem = (element, index) => ((SaveCard)element).ClearSaveEntry();
            saveListView.destroyItem = element => ((SaveCard)element).ClearSaveEntry();

            RequireElement<Button>(BackButtonName).clicked += () => BackClicked?.Invoke();
        }

        public event Action<string> SaveToLoadChosen;

        public event Action BackClicked;

        protected override void HandlePageShown()
        {
            RefreshSaveList();
        }

        private void RefreshSaveList()
        {
            saveEntries = SaveLibrary.ReadAllEntries();
            saveListView.itemsSource = saveEntries;
            saveListView.RefreshItems();

            bool hasSaves = saveEntries.Count > 0;
            saveListView.style.display = hasSaves ? DisplayStyle.Flex : DisplayStyle.None;
            emptyLabel.style.display = hasSaves ? DisplayStyle.None : DisplayStyle.Flex;

            Log.Info($"Saves page lists {saveEntries.Count} saves");
        }

        private void AskToLoad(SaveEntry saveEntry)
        {
            if (!saveEntry.IsFromAnotherVersion)
            {
                ChooseSaveToLoad(saveEntry);
                return;
            }

            dialog.ShowDangerousQuestion(ForeignVersionQuestion, $"Сохранение {saveEntry.Meta.Version}, игра {Application.version}. Совместимость не гарантируется.", ForeignVersionConfirmLabel, () => ChooseSaveToLoad(saveEntry));
        }

        private void ChooseSaveToLoad(SaveEntry saveEntry)
        {
            SaveToLoadChosen?.Invoke(saveEntry.Location);
        }

        private void AskToDelete(SaveEntry saveEntry)
        {
            dialog.ShowDangerousQuestion(DeleteQuestion, RussianDate.FormatDateAndTime(saveEntry.Meta.Stamp), DeleteConfirmLabel, () => DeleteSave(saveEntry));
        }

        private void DeleteSave(SaveEntry saveEntry)
        {
            SaveLibrary.DeleteSave(saveEntry);
            RefreshSaveList();
        }
    }
}
