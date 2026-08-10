using Shooter.Game.Body.Notifying;
using Shooter.Game.Loot;
using UnityEngine;
using Environment = Shooter.Game.Environment;

namespace Shooter.Client.Interface.Notifying
{
    public sealed class ItemsGivenLine : NotificationLine<ItemsGivenNotification>
    {
        protected override Sprite Icon(ItemsGivenNotification notification)
        {
            ItemSpec spec = Spec(notification);

            return spec == null || spec.Icon == null ? null : spec.Icon.Sprite;
        }

        protected override string Title(ItemsGivenNotification notification)
        {
            ItemSpec spec = Spec(notification);
            string title = spec == null ? notification.ItemSpecId : spec.Title;

            return spec is UniqueItemSpec ? title : $"{title} ×{notification.Amount}";
        }

        protected override long Actor(ItemsGivenNotification notification) => notification.ActorId;

        private static ItemSpec Spec(ItemsGivenNotification notification)
        {
            ItemCatalog catalog = Environment.Current == null ? null : Environment.Current.Items;

            return catalog == null ? null : catalog.Spec(notification.ItemSpecId);
        }
    }
}
