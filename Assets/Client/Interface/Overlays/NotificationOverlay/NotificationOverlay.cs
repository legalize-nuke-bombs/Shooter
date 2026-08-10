using Shooter.Client.Interface.Notifying;
using Shooter.Client.Playing;
using Shooter.Game.Body.EarSounding;
using Shooter.Game.Body.Notifying;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using Environment = Shooter.Game.Environment;

namespace Shooter.Client.Interface.Overlays
{
    public class NotificationOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string FeedElement = "notifications";
        private const long Life = 5000;
        private const int Limit = 4;

        private readonly PlayerNames names = new PlayerNames();

        private VisualElement feed;
        private PlayerNotificationRecipient recipient;

        private void Update()
        {
            if (!Bound) return;

            PlayerNotificationRecipient own = OwnPlayer.Find<PlayerNotificationRecipient>();
            if (own == recipient) return;

            Forget();
            recipient = own;

            if (recipient != null) recipient.Shown += Show;
        }

        protected override bool Bind(VisualElement root)
        {
            feed = root.Q<VisualElement>(FeedElement);

            if (feed == null)
            {
                Log.Error($"Overlay document has no {FeedElement} element, notifications stay hidden");
                return false;
            }

            feed.Clear();

            return true;
        }

        protected override void Unbind()
        {
            Forget();
            feed = null;
        }

        private void Show(Notification notification)
        {
            NotificationSpec spec = Spec(notification);
            if (spec == null || string.IsNullOrEmpty(spec.Title)) return;

            VisualElement element = Line(notification, spec);
            feed.Add(element);

            while (feed.childCount > Limit) feed.RemoveAt(0);

            feed.schedule.Execute(element.RemoveFromHierarchy).StartingIn(Life);

            Ring(Sound(notification, spec));
        }

        private VisualElement Line(Notification notification, NotificationSpec spec)
        {
            var line = new VisualElement();
            line.AddToClassList("notification");

            Sprite image = Icon(notification, spec);

            if (image != null)
            {
                var box = new VisualElement();
                box.AddToClassList("notification__icon");
                box.style.backgroundImage = Background.FromSprite(image);
                line.Add(box);
            }

            var body = new VisualElement();
            body.AddToClassList("notification__body");

            var caption = new Label(Template.Filled(spec.Title, notification, names));
            caption.AddToClassList("line");
            caption.AddToClassList("notification__title");
            body.Add(caption);

            string under = Template.Filled(spec.Subtitle, notification, names);

            if (!string.IsNullOrEmpty(under))
            {
                var from = new Label(under);
                from.AddToClassList("notification__from");
                body.Add(from);
            }

            line.Add(body);

            return line;
        }

        private static NotificationSpec Spec(Notification notification)
        {
            NotificationCatalog catalog = Environment.Current == null ? null : Environment.Current.Notifications;

            return catalog == null ? null : catalog.Of(notification.Spec);
        }

        private static Sprite Icon(Notification notification, NotificationSpec spec)
        {
            if (!notification.Icon.IsEmpty && Environment.Current != null && Environment.Current.Icons != null)
            {
                Sprite own = Environment.Current.Icons.Sprite(notification.Icon);
                if (own != null) return own;
            }

            return spec.Icon == null ? null : spec.Icon.Sprite;
        }

        private static EarSoundSpec Sound(Notification notification, NotificationSpec spec)
        {
            if (!notification.Sound.IsEmpty && Environment.Current != null && Environment.Current.EarSounds != null)
            {
                EarSoundSpec own = Environment.Current.EarSounds.Of(notification.Sound);
                if (own != null) return own;
            }

            return spec.Sound;
        }

        private void Ring(EarSoundSpec sound)
        {
            if (sound == null) return;

            EarSpeaker ear = OwnPlayer.Find<EarSpeaker>();
            if (ear == null) return;

            ear.PlayLocal(sound);
        }

        private void Forget()
        {
            if (recipient == null) return;

            recipient.Shown -= Show;
            recipient = null;
        }
    }
}
