using System;
using Shooter.Client.Interface.Naming;
using Shooter.Game.Body;
using Shooter.Game.Body.EarSounding;
using Shooter.Game.Body.Notifying;
using Shooter.Game.Identity;
using UnityEngine;
using UnityEngine.UIElements;
using Environment = Shooter.Game.Environment;

namespace Shooter.Client.Interface.Overlays
{
    public abstract class NotificationLine
    {
        public abstract Type Kind { get; }

        public abstract VisualElement Build(Notification notification);

        public abstract EarSoundSpec Sound(Notification notification);
    }

    public abstract class NotificationLine<T> : NotificationLine where T : Notification
    {
        private const string Stranger = "Незнакомец";

        private readonly NameMapper mapper = new NameMapper();

        public override Type Kind => typeof(T);

        public override VisualElement Build(Notification notification) => Assemble((T)notification);

        public override EarSoundSpec Sound(Notification notification) => Sound((T)notification);

        protected abstract Sprite Icon(T notification);

        protected abstract string Title(T notification);

        protected abstract long Actor(T notification);

        protected abstract EarSoundSpec Sound(T notification);

        protected static EarSoundCatalog Sounds => Environment.Current == null ? null : Environment.Current.EarSounds;

        private VisualElement Assemble(T notification)
        {
            var line = new VisualElement();
            line.AddToClassList("notification");

            Sprite icon = Icon(notification);
            if (icon != null)
            {
                var image = new VisualElement();
                image.AddToClassList("notification__icon");
                image.style.backgroundImage = Background.FromSprite(icon);
                line.Add(image);
            }

            var body = new VisualElement();
            body.AddToClassList("notification__body");

            var caption = new Label(Title(notification));
            caption.AddToClassList("line");
            caption.AddToClassList("notification__title");
            body.Add(caption);

            var from = new Label($"от {Named(Actor(notification))}");
            from.AddToClassList("notification__from");
            body.Add(from);

            line.Add(body);

            return line;
        }

        private string Named(long actorId)
        {
            PersistentId actor = Environment.Current == null ? null : Environment.Current.PersistentIds.Of(actorId);
            if (actor == null) return Stranger;

            var nameable = actor.GetComponentInChildren<Nameable>();
            if (nameable == null) return Stranger;

            string named = mapper.Of(nameable);

            return string.IsNullOrEmpty(named) ? Stranger : named;
        }
    }
}
