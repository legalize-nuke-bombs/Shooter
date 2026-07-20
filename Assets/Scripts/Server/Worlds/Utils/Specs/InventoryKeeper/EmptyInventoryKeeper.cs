using Shooter.Server.Worlds.Utils.Inventories;
using Shooter.Server.Worlds.Utils.Items;

namespace Shooter.Server.Worlds.Utils.Specs.InventoryKeeper
{
    public class EmptyInventoryKeeper : IInventoryKeeper
    {
        public void Take(StackableItem item, int amount)
        {

        }

        public void Take(UniqueItem item)
        {

        }

        public UniqueItem Equipted()
        {
            return null;
        }

        public bool Equip(long uniqueItemId)
        {
            return false;
        }

        public int Amount(StackableItem item)
        {
            return 0;
        }

        public int Drop(StackableItem item, int amount, InventoryOnConflictAction action)
        {
            return 0;
        }

        public InventoryState State()
        {
            return new InventoryState();
        }
    }
}
