using System;
using System.Collections.Generic;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Utils.Items;

namespace Shooter.Server.Worlds.Utils.Inventories
{
    public class Inventory : Part
    {
        private readonly Dictionary<StackableItem, int> stacks = new Dictionary<StackableItem, int>();
        private readonly Dictionary<long, UniqueItem> unique = new Dictionary<long, UniqueItem>();
        private long? equiptedId = null;

        public void Add(StackableItem item, int amount)
        {
            stacks[item] = stacks.GetValueOrDefault(item, 0) + amount;
        }

        public void Add(UniqueItem item)
        {
            unique.Add(item.Id, item);
        }

        public int Amount(StackableItem item)
        {
            return stacks.GetValueOrDefault(item, 0);
        }

        public int Remove(StackableItem item, int amount, InventoryOnConflictAction action)
        {
            int current = stacks.GetValueOrDefault(item, 0);

            switch (action)
            {
                case InventoryOnConflictAction.Rollback:
                    if (current >= amount)
                    {
                        stacks[item] = current - amount;
                        return amount;
                    }
                    return 0;
                case InventoryOnConflictAction.Partly:
                    int toRemove = Math.Min(current, amount);
                    stacks[item] = current - toRemove;
                    return toRemove;
            }

            Log.Error("Unexpected InventoryOnConflictAction {}", action);
            return 0;
        }

        public UniqueItem Equipted()
        {
            if (equiptedId == null)
            {
                return null;
            }

            return unique.GetValueOrDefault(equiptedId.Value, null);
        }

        public bool Equip(long uniqueItemId)
        {
            if (unique.ContainsKey(uniqueItemId))
            {
                equiptedId = uniqueItemId;
                return true;
            }

            return false;
        }

        public override InventoryState State()
        {
            var uniqueStates = new Dictionary<long, UniqueItemState>();
            foreach (UniqueItem item in unique.Values)
            {
                uniqueStates.Add(item.Id, item.State());
            }

            return new InventoryState
            {
                Stacks = new Dictionary<StackableItem, int>(stacks),
                Unique = uniqueStates,
                EquiptedId = equiptedId
            };
        }
    }
}
