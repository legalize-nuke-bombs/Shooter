using System;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class SaveCard : VisualElement
    {
        private const string CardClass = "save";
        private const string PreviewClass = "save__preview";
        private const string InfoClass = "save__info";
        private const string StampClass = "save__stamp";
        private const string VersionClass = "save__version";
        private const string ForeignClass = "save__version--foreign";
        private const string ActionsClass = "save__actions";
        private const string ActionClass = "save__action";
        private const string LoadButtonText = "Загрузить";
        private const string DeleteButtonText = "Удалить";

        private readonly VisualElement previewElement;
        private readonly Label stampLabel;
        private readonly Label versionLabel;
        private SaveEntry shownSaveEntry;
        private Action<SaveEntry> deleteCallback;
        private Action<SaveEntry> loadCallback;
        private Texture2D previewTexture;

        public SaveCard()
        {
            AddToClassList(CardClass);

            previewElement = new VisualElement();
            previewElement.AddToClassList(PreviewClass);
            Add(previewElement);

            VisualElement infoElement = new();
            infoElement.AddToClassList(InfoClass);
            Add(infoElement);

            stampLabel = new Label();
            stampLabel.AddToClassList(StampClass);
            infoElement.Add(stampLabel);

            versionLabel = new Label();
            versionLabel.AddToClassList(VersionClass);
            infoElement.Add(versionLabel);

            VisualElement actionsElement = new();
            actionsElement.AddToClassList(ActionsClass);
            Add(actionsElement);

            Button deleteButton = new(() => deleteCallback?.Invoke(shownSaveEntry)) { text = DeleteButtonText };
            deleteButton.AddToClassList(ActionClass);
            actionsElement.Add(deleteButton);

            Button loadButton = new(() => loadCallback?.Invoke(shownSaveEntry)) { text = LoadButtonText };
            loadButton.AddToClassList(ActionClass);
            actionsElement.Add(loadButton);
        }

        public void ShowSaveEntry(SaveEntry saveEntry, Action<SaveEntry> loadCallback, Action<SaveEntry> deleteCallback)
        {
            ClearSaveEntry();

            shownSaveEntry = saveEntry;
            this.loadCallback = loadCallback;
            this.deleteCallback = deleteCallback;

            stampLabel.text = RussianDate.FormatDateAndTime(saveEntry.Meta.Stamp);
            versionLabel.text = saveEntry.Meta.Version;
            versionLabel.EnableInClassList(ForeignClass, saveEntry.IsFromAnotherVersion);

            byte[] previewBytes = saveEntry.ReadPreview();
            if (previewBytes == null) return;

            previewTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (previewTexture.LoadImage(previewBytes)) previewElement.style.backgroundImage = new StyleBackground(previewTexture);
        }

        public void ClearSaveEntry()
        {
            shownSaveEntry = null;
            loadCallback = null;
            deleteCallback = null;
            previewElement.style.backgroundImage = new StyleBackground(StyleKeyword.Null);

            if (previewTexture == null) return;

            UnityEngine.Object.Destroy(previewTexture);
            previewTexture = null;
        }
    }
}
