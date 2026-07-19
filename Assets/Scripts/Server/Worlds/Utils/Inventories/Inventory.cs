using System.Collections.Generic;
using Shooter.Server.Worlds.Utils.Items;

namespace Shooter.Server.Worlds.Utils.Inventories
{
    public class Inventory
    {
        private readonly Dictionary<StackableItem, int> stacks = new Dictionary<StackableItem, int>();
        private readonly List<UniqueItem> unique = new List<UniqueItem>();

        public void Add(StackableItem item, int amount)
        {
            stacks[item] = stacks.GetValueOrDefault(item) + amount;
        }

        public void Add(UniqueItem item)
        {
            unique.Add(item);
        }

        public InventoryState State()
        {
            var uniqueStates = new List<UniqueItemState>();
            foreach (UniqueItem item in unique)
            {
                uniqueStates.Add(item.State());
            }

            return new InventoryState
            {
                Stacks = new Dictionary<StackableItem, int>(stacks),
                Unique = uniqueStates
            };
        }
    }
}
