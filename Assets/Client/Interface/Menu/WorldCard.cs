using System;
using Shooter.Game.Core.Saves;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class WorldCard : VisualElement
    {
        private const string CardClass = "world";
        private const string PreviewClass = "world__preview";
        private const string InfoClass = "world__info";
        private const string StampClass = "world__stamp";
        private const string ClockClass = "world__clock";
        private const string VersionClass = "world__version";
        private const string ForeignClass = "world__version--foreign";
        private const string ActionsClass = "world__actions";
        private const string ActionClass = "world__action";
        private const string ArmedClass = "world__action--armed";
        private const string LoadText = "Загрузить";
        private const string DeleteText = "Удалить";
        private const string ConfirmText = "Точно?";
        private const long ArmedFor = 3000;

        private readonly Label clock;
        private readonly Button delete;
        private readonly VisualElement preview;
        private readonly Label stamp;
        private readonly Label version;
        private bool armed;
        private IVisualElementScheduledItem disarming;
        private SaveEntry entry;
        private Action<SaveEntry> onDelete;
        private Action<SaveEntry> onLoad;
        private Texture2D texture;

        public WorldCard()
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

            clock = new Label();
            clock.AddToClassList(ClockClass);
            info.Add(clock);

            version = new Label();
            version.AddToClassList(VersionClass);
            info.Add(version);

            var actions = new VisualElement();
            actions.AddToClassList(ActionsClass);
            Add(actions);

            var load = new Button(() => onLoad?.Invoke(entry)) { text = LoadText };
            load.AddToClassList(ActionClass);
            actions.Add(load);

            delete = new Button(Delete) { text = DeleteText };
            delete.AddToClassList(ActionClass);
            actions.Add(delete);
        }

        public void Show(SaveEntry shown, Action<SaveEntry> load, Action<SaveEntry> remove)
        {
            Release();

            entry = shown;
            onLoad = load;
            onDelete = remove;

            stamp.text = RussianDate.Moment(shown.Meta.Stamp);
            clock.text = "В мире " + RussianDate.Moment(shown.Meta.Clock);

            bool foreign = shown.Meta.Version != Application.version;
            version.text = foreign
                ? $"Версия {shown.Meta.Version}, сейчас {Application.version}"
                : $"Версия {shown.Meta.Version}";
            version.EnableInClassList(ForeignClass, foreign);

            byte[] bytes = shown.ReadPreview();
            if (bytes == null) return;

            texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (texture.LoadImage(bytes)) preview.style.backgroundImage = new StyleBackground(texture);
        }

        public void Release()
        {
            Disarm();
            entry = null;
            onLoad = null;
            onDelete = null;
            preview.style.backgroundImage = new StyleBackground(StyleKeyword.Null);

            if (texture == null) return;

            UnityEngine.Object.Destroy(texture);
            texture = null;
        }

        private void Delete()
        {
            if (!armed)
            {
                armed = true;
                delete.text = ConfirmText;
                delete.AddToClassList(ArmedClass);
                disarming = schedule.Execute(Disarm).StartingIn(ArmedFor);
                return;
            }

            SaveEntry doomed = entry;
            Disarm();
            onDelete?.Invoke(doomed);
        }

        private void Disarm()
        {
            disarming?.Pause();
            disarming = null;
            armed = false;
            delete.text = DeleteText;
            delete.RemoveFromClassList(ArmedClass);
        }
    }
}
