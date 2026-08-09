using Shooter.Game.Identity;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [RequireComponent(typeof(Inventory))]
    public class InventoryExchanger : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static int characterMask;
        private static int CharacterMask => characterMask != 0 ? characterMask : characterMask = LayerMask.GetMask("Character");

        [SerializeField] private float exchangeRadius = 10f;
        public float ExchangeRadius => exchangeRadius;

        private readonly Collider[] around = new Collider[64];

        private Inventory inventory;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
        }

        public bool GiveStackable(long targetId, ItemSpec stackable, int amount)
        {
            Inventory targetInventory = TargetInventory(targetId);
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

            Log.Info($"{name} gave {stackable.Id} x {amount} to {targetId}");
            return true;
        }

        public bool GiveUnique(long targetId, int slotId)
        {
            Inventory targetInventory = TargetInventory(targetId);
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

            Log.Info($"{name} gave unique item slot {slotId} ({uniqueItem.SpecId}) to {targetId}");
            return true;
        }

        private Inventory TargetInventory(long targetId)
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, exchangeRadius, around, CharacterMask);
            if (hits == around.Length)
                Log.Warn($"{name} sees {hits} characters within {exchangeRadius}m, somebody stays invisible for the exchange");

            for (int i = 0; i < hits; i++)
            {
                PersistentId id = around[i].GetComponentInParent<PersistentId>();
                if (id == null || id.Value != targetId || id.transform == transform) continue;

                return id.GetComponent<Inventory>();
            }

            return null;
        }
    }
}
