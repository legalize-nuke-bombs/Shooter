using Shooter.Game;
using Shooter.Game.Body.EarSounding;
using Shooter.Game.Body.Notifying;
using Shooter.Game.Loot;
using UnityEngine;

namespace Shooter.Client.Interface.Overlays
{
    public sealed class ItemsGivenLine : NotificationLine<ItemsGivenNotification>
    {
        private const string SoundId = "items_given";

        protected override Sprite Icon(ItemsGivenNotification notification)
        {
            ItemSpec spec = Spec(notification);

            return spec == null ? null : spec.Icon;
        }

        protected override string Title(ItemsGivenNotification notification)
        {
            ItemSpec spec = Spec(notification);
            string title = spec == null ? notification.ItemSpecId : spec.Title;

            return spec != null && !spec.Stackable ? title : $"{title} ×{notification.Amount}";
        }

        protected override long Actor(ItemsGivenNotification notification) => notification.ActorId;

        protected override EarSoundSpec Sound(ItemsGivenNotification notification) => Sounds == null ? null : Sounds.Of(SoundId);

        private static ItemSpec Spec(ItemsGivenNotification notification)
        {
            ItemCatalog catalog = Environment.Current == null ? null : Environment.Current.Items;

            return catalog == null ? null : catalog.Spec(notification.ItemSpecId);
        }
    }
}
