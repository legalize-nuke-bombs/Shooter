using Shooter.Client.Interface.Naming;
using Shooter.Client.Playing;
using Shooter.Game;
using Shooter.Game.Body;
using Shooter.Game.Body.Notifying;
using Shooter.Game.Identity;
using Shooter.Game.Loot;
using Shooter.Logging;
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
                _ => null
            };
        }

        private VisualElement Given(ItemsGivenNotification given)
        {
            ItemCatalog catalog = Environment.Current == null ? null : Environment.Current.Items;
            ItemSpec spec = catalog == null ? null : catalog.Spec(given.ItemSpecId);

            var line = new VisualElement();
            line.AddToClassList("notification");

            if (spec != null && spec.Icon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("notification__icon");
                icon.style.backgroundImage = Background.FromSprite(spec.Icon);
                line.Add(icon);
            }

            var body = new VisualElement();
            body.AddToClassList("notification__body");

            var title = new Label(Told(given, spec));
            title.AddToClassList("line");
            title.AddToClassList("notification__title");
            body.Add(title);

            var from = new Label($"от {Named(given.ActorId)}");
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
