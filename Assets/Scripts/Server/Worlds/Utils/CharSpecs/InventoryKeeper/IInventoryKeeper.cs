using Shooter.Server.Worlds.Utils.Inventories;
using Shooter.Server.Worlds.Utils.Items;

namespace Shooter.Server.Worlds.Utils.CharSpecs.InventoryKeeper
{
    public interface IInventoryKeeper
    {
        void Take(StackableItem item, int amount);

        void Take(UniqueItem item);

        UniqueItem Equipted();

        int Amount(StackableItem item);

        int Drop(StackableItem item, int amount, InventoryOnConflictAction action);

        InventoryState State();
    }
}
