using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class NotificationOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string FeedElement = "notifications";
        private const long Life = 5000;
        private const int Limit = 4;


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
            NotificationSpec spec = Catalogs.Of<NotificationCatalog>().Of(notification.Spec);
            if (spec == null || string.IsNullOrEmpty(spec.Title)) return;

            VisualElement element = Line(notification);
            feed.Add(element);

            while (feed.childCount > Limit) feed.RemoveAt(0);

            feed.schedule.Execute(element.RemoveFromHierarchy).StartingIn(Life);

            Ring(notification.Sound());
        }

        private VisualElement Line(Notification notification)
        {
            var line = new VisualElement();
            line.AddToClassList("notification");

            IconSpec icon = notification.Icon();
            Sprite image = icon == null ? null : icon.Sprite;

            if (image != null)
            {
                var box = new VisualElement();
                box.AddToClassList("notification__icon");
                box.style.backgroundImage = Background.FromSprite(image);
                line.Add(box);
            }

            var body = new VisualElement();
            body.AddToClassList("notification__body");

            var caption = new Label(Template.Filled(notification.Title(), notification));
            caption.AddToClassList("line");
            caption.AddToClassList("notification__title");
            body.Add(caption);

            string under = Template.Filled(notification.Subtitle(), notification);

            if (!string.IsNullOrEmpty(under))
            {
                var from = new Label(under);
                from.AddToClassList("notification__from");
                body.Add(from);
            }

            line.Add(body);

            return line;
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
