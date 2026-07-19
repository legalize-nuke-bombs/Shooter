using Shooter.Server.Worlds.Utils.Inventories;
using Shooter.Server.Worlds.Utils.Items;

namespace Shooter.Server.Worlds.Utils.CharSpecs.InventoryKeeper
{
    public class EmptyInventoryKeeper : IInventoryKeeper
    {
        public void Take(StackableItem item, int amount)
        {

        }

        public void Take(UniqueItem item)
        {

        }

        public InventoryState State()
        {
            return new InventoryState();
        }
    }
}
