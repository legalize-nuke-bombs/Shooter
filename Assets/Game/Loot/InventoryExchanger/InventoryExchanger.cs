using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Loot.InventoryExchanger
{
    [RequireComponent(typeof(Inventory))]
    public class InventoryExchanger : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float exchangeRadius = 5f;
        private readonly Collider[] around = new Collider[512];

        private Inventory inventory;

        public void Awake()
        {
            inventory = GetComponent<Inventory>();
        }

        public bool GiveStackable(long targetId, ItemSpec stackable, int amount)
        {
            Inventory targetInventory = TargetInventory(targetId);
            if (targetInventory == null)
            {
                Log.Info("Failed to give {} x {} from {} to {} : target does not have inventory", stackable.Id, amount, name, targetId);
                return false;
            }

            if (inventory.Remove(stackable, amount, InventoryOnConflict.Rollback) != amount)
            {
                Log.Info("Failed to give {} x {} from {} to {} : insufficient items", stackable.Id, amount, name, targetId);
                return false;
            }
            targetInventory.Add(stackable, amount);

            Log.Info("{} gave {} x {} to {}", name, stackable.Id, amount, targetId);
            return true;
        }

        public bool GiveUnique(long targetId, int slotId)
        {
            Inventory targetInventory = TargetInventory(targetId);
            if (targetInventory == null)
            {
                Log.Info("Failed to give unique slot {} from {} to {}: target does not have inventory", slotId, name, targetId);
                return false;
            }

            UniqueItem uniqueItem = inventory.Take(slotId);
            if (uniqueItem == null)
            {
                Log.Info("Failed to give unique slot {} from {} to {}: the specified unique item does not exist", slotId, name, targetId);
                return false;
            }
            targetInventory.Put(uniqueItem);

            Log.Info("{} gave unique item slot {} ({}) to {}", name, slotId, uniqueItem.SpecId, targetId);
            return true;
        }

        private Inventory TargetInventory(long targetId)
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, exchangeRadius, around);
            if (hits == around.Length)
                Log.Warn("OverlapSphereNonAlloc overflow");

            for (int i = 0; i < hits; i++)
            {
                Inventory targetInventory = around[i].GetComponentInParent<Inventory>();
                if (targetInventory != null)
                {
                    return targetInventory;
                }
            }

            return null;
        }
    }
}
