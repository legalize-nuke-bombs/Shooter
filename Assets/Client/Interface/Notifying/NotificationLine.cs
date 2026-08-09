using System;
using Shooter.Client.Interface.Naming;
using Shooter.Game.Body;
using Shooter.Game.Body.EarSounding;
using Shooter.Game.Body.Notifying;
using Shooter.Game.Identity;
using UnityEngine;
using UnityEngine.UIElements;
using Environment = Shooter.Game.Environment;

namespace Shooter.Client.Interface.Notifying
{
    public abstract class NotificationLine : MonoBehaviour
    {
        public abstract Type Kind { get; }

        public abstract VisualElement Build(Notification notification);

        public abstract EarSoundSpec Sound(Notification notification);
    }

    public abstract class NotificationLine<T> : NotificationLine where T : Notification
    {
        private const string Stranger = "Незнакомец";

        [SerializeField] private Sprite icon;
        [SerializeField] private EarSoundSpec sound;

        private readonly NameMapper mapper = new NameMapper();

        public override Type Kind => typeof(T);

        public override VisualElement Build(Notification notification) => Assemble((T)notification);

        public override EarSoundSpec Sound(Notification notification) => Sound((T)notification);

        protected virtual Sprite Icon(T notification) => icon;

        protected abstract string Title(T notification);

        protected abstract long Actor(T notification);

        protected virtual EarSoundSpec Sound(T notification) => sound;

        private VisualElement Assemble(T notification)
        {
            var line = new VisualElement();
            line.AddToClassList("notification");

            Sprite image = Icon(notification);
            if (image != null)
            {
                var box = new VisualElement();
                box.AddToClassList("notification__icon");
                box.style.backgroundImage = Background.FromSprite(image);
                line.Add(box);
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
