using System;
using System.Collections.Generic;
using Shooter.Client.Interface.Notifying;
using Shooter.Client.Playing;
using Shooter.Game.Body.EarSounding;
using Shooter.Game.Body.Notifying;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class NotificationOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string FeedElement = "notifications";
        private const long Life = 5000;
        private const int Limit = 4;

        private Dictionary<Type, NotificationLine> lines;
        private VisualElement feed;
        private PlayerNotificationRecipient recipient;

        private void Awake()
        {
            lines = Lines();
        }

        private Dictionary<Type, NotificationLine> Lines()
        {
            var known = new Dictionary<Type, NotificationLine>();

            foreach (NotificationLine line in GetComponents<NotificationLine>())
            {
                if (known.TryGetValue(line.Kind, out NotificationLine taken))
                {
                    Log.Error($"Notification lines {taken.GetType().Name} and {line.GetType().Name} both draw {line.Kind.Name}, the second one stays unused");
                    continue;
                }

                known.Add(line.Kind, line);
            }

            return known;
        }

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
            if (!lines.TryGetValue(notification.GetType(), out NotificationLine line)) return;

            VisualElement element = line.Build(notification);
            feed.Add(element);

            while (feed.childCount > Limit) feed.RemoveAt(0);

            feed.schedule.Execute(element.RemoveFromHierarchy).StartingIn(Life);

            Ring(line.Sound(notification));
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
