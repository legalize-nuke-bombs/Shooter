using Shooter.Game.Notifying;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Loot
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(PersistentId))]
    public class InventoryExchanger : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float exchangeRadius = 10f;
        public float ExchangeRadius => exchangeRadius;

        [SerializeField] private NotificationSpec itemsGiven;
        [SerializeField] private NotificationSpec itemGiven;

        private Inventory inventory;
        private PersistentId ownId;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            ownId = GetComponent<PersistentId>();
        }

        public bool GiveStackable(long targetId, StackableItemSpec stackable, int amount)
        {
            PersistentId target = Target(targetId);
            Inventory targetInventory = target == null ? null : target.GetComponent<Inventory>();
            if (targetInventory == null)
            {
                Log.Info($"Failed to give {stackable.Id} x {amount} from {name} to {targetId} : the target is not around");
                return false;
            }

            if (inventory.RemoveStackable(stackable, amount, InventoryOnConflict.Rollback) != amount)
            {
                Log.Info($"Failed to give {stackable.Id} x {amount} from {name} to {targetId} : insufficient items");
                return false;
            }
            targetInventory.AddStackable(stackable, amount);

            Notify(target, itemsGiven, stackable.Key, amount);

            Log.Info($"{name} gave {stackable.Id} x {amount} to {targetId}");
            return true;
        }

        public bool GiveUnique(long targetId, int slotId)
        {
            PersistentId target = Target(targetId);
            Inventory targetInventory = target == null ? null : target.GetComponent<Inventory>();
            if (targetInventory == null)
            {
                Log.Info($"Failed to give unique slot {slotId} from {name} to {targetId}: the target is not around");
                return false;
            }

            UniqueItem uniqueItem = inventory.Take(slotId);
            if (uniqueItem == null)
            {
                Log.Info($"Failed to give unique slot {slotId} from {name} to {targetId}: the specified unique item does not exist");
                return false;
            }
            targetInventory.Put(uniqueItem);

            Notify(target, itemGiven, uniqueItem.SpecId, 1);

            Log.Info($"{name} gave unique item slot {slotId} ({uniqueItem.SpecId}) to {targetId}");
            return true;
        }

        private void Notify(PersistentId target, NotificationSpec spec, string itemSpecId, int amount)
        {
            var recipient = target.GetComponent<MainNotificationRecipient>();
            if (recipient == null) return;

            if (spec == null)
            {
                Log.Warn($"{name} has no notification to tell {target.name} about {itemSpecId}, the gift goes unnoticed");
                return;
            }

            ItemSpec item = Environment.Current.Items.Spec(itemSpecId);

            recipient.Receive(spec.Notify()
                .Under(item == null ? null : item.Icon)
                .With(Args.Actor, ownId.Value)
                .With(Args.Subject, itemSpecId)
                .With("amount", amount));
        }

        private PersistentId Target(long targetId)
        {
            PersistentId target = Environment.Current.PersistentIds.Of(targetId);
            if (target == null || target == ownId) return null;

            return Vector3.Distance(target.transform.position, transform.position) <= exchangeRadius ? target : null;
        }
    }
}
