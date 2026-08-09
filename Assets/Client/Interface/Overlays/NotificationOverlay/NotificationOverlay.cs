using Shooter.Client.Interface.Naming;
using Shooter.Client.Playing;
using Shooter.Game;
using Shooter.Game.Body;
using Shooter.Game.Body.Notifying;
using Shooter.Game.Identity;
using Shooter.Game.Loot;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class NotificationOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string FeedElement = "notifications";
        private const string Stranger = "Незнакомец";
        private const long Life = 5000;
        private const int Limit = 4;

        private readonly NameMapper mapper = new NameMapper();

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
            VisualElement line = Line(notification);
            if (line == null) return;

            feed.Add(line);

            while (feed.childCount > Limit) feed.RemoveAt(0);

            feed.schedule.Execute(line.RemoveFromHierarchy).StartingIn(Life);
        }

        private VisualElement Line(Notification notification)
        {
            return notification switch
            {
                ItemsGivenNotification given => Given(given),
                RelationChangedNotification changed => Changed(changed),
                _ => null
            };
        }

        private VisualElement Given(ItemsGivenNotification given)
        {
            ItemCatalog catalog = Environment.Current == null ? null : Environment.Current.Items;
            ItemSpec spec = catalog == null ? null : catalog.Spec(given.ItemSpecId);

            return Line(spec == null ? null : spec.Icon, Told(given, spec), given.ActorId);
        }

        private VisualElement Changed(RelationChangedNotification changed)
        {
            return Line(null, changed.After > changed.Before ? "Отношение улучшилось" : "Отношение ухудшилось", changed.ActorId);
        }

        private VisualElement Line(Sprite icon, string title, long actorId)
        {
            var line = new VisualElement();
            line.AddToClassList("notification");

            if (icon != null)
            {
                var image = new VisualElement();
                image.AddToClassList("notification__icon");
                image.style.backgroundImage = Background.FromSprite(icon);
                line.Add(image);
            }

            var body = new VisualElement();
            body.AddToClassList("notification__body");

            var caption = new Label(title);
            caption.AddToClassList("line");
            caption.AddToClassList("notification__title");
            body.Add(caption);

            var from = new Label($"от {Named(actorId)}");
            from.AddToClassList("notification__from");
            body.Add(from);

            line.Add(body);

            return line;
        }

        private static string Told(ItemsGivenNotification given, ItemSpec spec)
        {
            string title = spec == null ? given.ItemSpecId : spec.Title;

            return spec != null && !spec.Stackable ? title : $"{title} ×{given.Amount}";
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

        private void Forget()
        {
            if (recipient == null) return;

            recipient.Shown -= Show;
            recipient = null;
        }
    }
}
