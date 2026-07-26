using System;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Digesting;
using Shooter.Logging;

namespace Shooter.Game.Items
{
    public class Inventory : NetworkBehaviour, IDigestible
    {
        private const int Nothing = -1;

        [SerializeField] private ItemCatalog catalog;

        private readonly NetworkList<Item> slots = new NetworkList<Item>(
            null, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> equipped = new NetworkVariable<int>(Nothing);

        public ItemCatalog Catalog => catalog;

        public int Count => slots.Count;

        public Item At(int slot)
        {
            return slots[slot];
        }

        public void Add(Item item)
        {
            if (!IsServer || item.Empty) return;

            ItemSpec spec = catalog == null ? null : catalog.Spec(item.Type);

            if (spec != null && spec.Stackable)
            {
                for (int slot = 0; slot < slots.Count; slot++)
                {
                    Item stack = slots[slot];
                    if (stack.Type != item.Type) continue;

                    slots[slot] = new Item(stack.Type, stack.Amount + item.Amount, stack.Magazine);
                    return;
                }
            }

            slots.Add(item);
            Log.Info("Entity {} took {} of {}", name, item.Amount, item.Type);
        }

        public int Amount(ItemType type)
        {
            int total = 0;

            foreach (Item item in slots)
            {
                if (item.Type == type) total += item.Amount;
            }

            return total;
        }

        public int Remove(ItemType type, int amount, InventoryOnConflict onConflict)
        {
            if (!IsServer || amount <= 0) return 0;

            int available = Amount(type);
            if (onConflict == InventoryOnConflict.Rollback && available < amount) return 0;

            int left = Math.Min(available, amount);
            int removed = left;

            for (int slot = slots.Count - 1; slot >= 0 && left > 0; slot--)
            {
                Item item = slots[slot];
                if (item.Type != type) continue;

                int taken = Math.Min(item.Amount, left);
                left -= taken;

                if (item.Amount == taken) Drop(slot);
                else slots[slot] = new Item(item.Type, item.Amount - taken, item.Magazine);
            }

            return removed;
        }

        public bool TryEquip(int slot)
        {
            if (!IsServer || slot < Nothing || slot >= slots.Count) return false;

            equipped.Value = slot;
            return true;
        }

        public bool Equipped(out Item item)
        {
            item = default;
            if (equipped.Value == Nothing || equipped.Value >= slots.Count) return false;

            item = slots[equipped.Value];
            return true;
        }

        public void Reequip(Item item)
        {
            if (!IsServer || equipped.Value == Nothing || equipped.Value >= slots.Count) return;

            slots[equipped.Value] = item;
        }

        public void DrainInto(Inventory target)
        {
            if (!IsServer) return;

            foreach (Item item in slots)
                target.Add(item);

            Clear();
        }

        public void Clear()
        {
            if (!IsServer) return;

            slots.Clear();
            equipped.Value = Nothing;
        }

        public string Digest()
        {
            if (!Equipped(out Item item)) return "Предмет в руках: -";

            ItemSpec spec = catalog == null ? null : catalog.Spec(item.Type);
            return "Предмет в руках: " + (spec == null ? item.Type.ToString() : spec.PromptName);
        }

        private void Drop(int slot)
        {
            slots.RemoveAt(slot);

            if (equipped.Value == slot) equipped.Value = Nothing;
            else if (equipped.Value > slot) equipped.Value--;
        }
    }
}
