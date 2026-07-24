using System;
using System.Collections.Generic;
using Shooter.Logging;
using Shooter.Server.Protocol;
using Shooter.Server.Worlds.Items;

namespace Shooter.Server.Worlds.Entities.Parts.Inventory
{
    public sealed class Inventory : Part
    {
        private readonly Dictionary<StackableItem, int> stacks = new Dictionary<StackableItem, int>();
        private readonly Dictionary<long, UniqueItem> unique = new Dictionary<long, UniqueItem>();
        private long? equippedId;

        public Inventory(Entity self) : base(self, typeof(Inventory))
        {
        }

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

        public void DrainInto(Inventory target)
        {
            foreach (KeyValuePair<StackableItem, int> stack in stacks)
                target.Add(stack.Key, stack.Value);

            foreach (UniqueItem item in unique.Values)
                target.Add(item);

            if (equippedId != null) target.TryEquip(equippedId.Value);

            Clear();
        }

        public void Clear()
        {
            stacks.Clear();
            unique.Clear();
            equippedId = null;
        }

        public UniqueItem Equipped()
        {
            if (equippedId == null) return null;

            return unique.TryGetValue(equippedId.Value, out UniqueItem item) ? item : null;
        }

        public bool TryEquip(long uniqueItemId)
        {
            if (!unique.ContainsKey(uniqueItemId)) return false;

            equippedId = uniqueItemId;
            return true;
        }

        public override void Apply(PlayerIntent input)
        {
        }

        public override void Tick(float dt)
        {
        }

        public override void Died()
        {
        }

        public override string Digest()
        {
            UniqueItem equipped = Equipped();
            return "Предмет в руках: " + (equipped == null ? "-" : equipped.GetType().Name);
        }

        public override PartState State()
        {
            var uniqueStates = new Dictionary<long, UniqueItemState>();
            foreach (UniqueItem item in unique.Values)
                uniqueStates.Add(item.Id, item.State());

            return new InventoryState
            {
                Stacks = new Dictionary<StackableItem, int>(stacks),
                Unique = uniqueStates,
                EquippedId = equippedId
            };
        }
    }
}
