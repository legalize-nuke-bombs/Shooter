using System;
using Shooter.Game.Core.Saves;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
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
        private const string LoadText = "Загрузить";
        private const string DeleteText = "Удалить";

        private readonly VisualElement preview;
        private readonly Label stamp;
        private readonly Label version;
        private SaveEntry entry;
        private Action<SaveEntry> onDelete;
        private Action<SaveEntry> onLoad;
        private Texture2D texture;

        public SaveCard()
        {
            AddToClassList(CardClass);

            preview = new VisualElement();
            preview.AddToClassList(PreviewClass);
            Add(preview);

            var info = new VisualElement();
            info.AddToClassList(InfoClass);
            Add(info);

            stamp = new Label();
            stamp.AddToClassList(StampClass);
            info.Add(stamp);

            version = new Label();
            version.AddToClassList(VersionClass);
            info.Add(version);

            var actions = new VisualElement();
            actions.AddToClassList(ActionsClass);
            Add(actions);

            var delete = new Button(() => onDelete?.Invoke(entry)) { text = DeleteText };
            delete.AddToClassList(ActionClass);
            actions.Add(delete);

            var load = new Button(() => onLoad?.Invoke(entry)) { text = LoadText };
            load.AddToClassList(ActionClass);
            actions.Add(load);
        }

        public void Show(SaveEntry shown, Action<SaveEntry> load, Action<SaveEntry> remove)
        {
            Release();

            entry = shown;
            onLoad = load;
            onDelete = remove;

            stamp.text = RussianDate.Moment(shown.Meta.Stamp);
            version.text = shown.Meta.Version;
            version.EnableInClassList(ForeignClass, shown.Foreign);

            byte[] bytes = shown.ReadPreview();
            if (bytes == null) return;

            texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (texture.LoadImage(bytes)) preview.style.backgroundImage = new StyleBackground(texture);
        }

        public void Release()
        {
            entry = null;
            onLoad = null;
            onDelete = null;
            preview.style.backgroundImage = new StyleBackground(StyleKeyword.Null);

            if (texture == null) return;

            UnityEngine.Object.Destroy(texture);
            texture = null;
        }
    }
}
