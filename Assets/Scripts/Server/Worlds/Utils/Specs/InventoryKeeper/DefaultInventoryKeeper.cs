using Shooter.Logging;
using Shooter.Server.Worlds.Utils.Inventories;
using Shooter.Server.Worlds.Utils.Items;

namespace Shooter.Server.Worlds.Utils.Specs.InventoryKeeper
{
    public class DefaultInventoryKeeper : IInventoryKeeper
    {
        private readonly Inventory inventory;

        public DefaultInventoryKeeper(Inventory inventory)
        {
            this.inventory = inventory;
        }

        public void Take(StackableItem item, int amount)
        {
            inventory.Add(item, amount);
        }

        public void Take(UniqueItem item)
        {
            inventory.Add(item);
        }

        public UniqueItem Equipted()
        {
            return inventory.Equipted();
        }

        public bool Equip(long uniqueItemId)
        {
            return inventory.Equip(uniqueItemId);
        }

        public int Amount(StackableItem item)
        {
            return inventory.Amount(item);
        }

        public int Drop(StackableItem item, int amount, InventoryOnConflictAction action)
        {
            return inventory.Remove(item, amount, action);
        }

        public InventoryState State()
        {
            return inventory.State();
        }
    }
}
