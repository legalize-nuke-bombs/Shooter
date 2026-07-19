using Shooter.Server.Worlds.Utils.Inventories;
using Shooter.Server.Worlds.Utils.Items;

namespace Shooter.Server.Worlds.Utils.CharSpecs.InventoryKeeper
{
    public class DefaultInventoryKeeper : IInventoryKeeper
    {
        private readonly Inventory inventory = new Inventory();

        public void Take(StackableItem item, int amount)
        {
            inventory.Add(item, amount);
        }

        public void Take(UniqueItem item)
        {
            inventory.Add(item);
        }

        public InventoryState State()
        {
            return inventory.State();
        }
    }
}
