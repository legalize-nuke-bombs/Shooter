using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Notifying;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    // The feed in the corner of the screen: it knows no domains, whoever wants the player's attention hands it a toast
    public class NotificationOverlay : Overlay
    {
        private const string FeedElement = "notifications";
        private const long Life = 5000;
        private const int Limit = 4;
        private static readonly Journal Log = Logs.Here();

        private VisualElement feed;
        private PlayerNotificationRecipient recipient;

        public static NotificationOverlay Current { get; private set; }

        private void Awake()
        {
            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            if (!Bound) return;

            PlayerNotificationRecipient own = OwnPlayer.Find<PlayerNotificationRecipient>();
            if (own == recipient) return;

            Forget();
            recipient = own;

            if (recipient != null) recipient.Shown += Relay;
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

        public void Show(Toast toast)
        {
            if (!Bound || string.IsNullOrEmpty(toast.Title)) return;

            VisualElement element = Line(toast);
            feed.Add(element);

            while (feed.childCount > Limit) feed.RemoveAt(0);

            feed.schedule.Execute(element.RemoveFromHierarchy).StartingIn(Life);

            Ring(toast.Sound);
        }

        // A server notification with arguments, until those move to observers of their own domains
        private void Relay(Notification notification)
        {
            Show(new Toast(
                notification.Icon(),
                notification.Sound(),
                Template.Filled(notification.Title(), notification),
                Template.Filled(notification.Subtitle(), notification)));
        }

        private static VisualElement Line(Toast toast)
        {
            var line = new VisualElement();
            line.AddToClassList("notification");

            Sprite image = toast.Icon == null ? null : toast.Icon.Sprite;

            if (image != null)
            {
                var box = new VisualElement();
                box.AddToClassList("notification__icon");
                box.style.backgroundImage = Background.FromSprite(image);
                line.Add(box);
            }

            var body = new VisualElement();
            body.AddToClassList("notification__body");

            var caption = new Label(toast.Title);
            caption.AddToClassList("line");
            caption.AddToClassList("notification__title");
            body.Add(caption);

            if (!string.IsNullOrEmpty(toast.Subtitle))
            {
                var from = new Label(toast.Subtitle);
                from.AddToClassList("notification__from");
                body.Add(from);
            }

            line.Add(body);

            return line;
        }

        private static void Ring(EarSoundSpec sound)
        {
            if (sound == null) return;

            EarSpeaker ear = OwnPlayer.Find<EarSpeaker>();
            if (ear == null) return;

            ear.PlayLocal(sound);
        }

        private void Forget()
        {
            if (recipient == null) return;

            recipient.Shown -= Relay;
            recipient = null;
        }
    }
}
