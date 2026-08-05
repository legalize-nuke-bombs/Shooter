using System;
using System.Text;
using Shooter.Game.Body;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Loot
{
    public class Inventory : NetworkBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        public const int Nothing = -1;

        [SerializeField] private ItemCatalog catalog;

        [SerializeField] private Entry[] contents;

        private readonly NetworkList<Item> slots = new NetworkList<Item>(
            null, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        [SerializeField] private NetworkVariable<int> equipped = new NetworkVariable<int>(Nothing);

        public ItemCatalog Catalog => catalog != null
            ? catalog
            : Environment.Current == null ? null : Environment.Current.Items;

        public event Action Changed;

        public int Count => slots.Count;

        public int EquippedSlot => equipped.Value;

        public override void OnNetworkSpawn()
        {
            slots.OnListChanged += Shifted;
            equipped.OnValueChanged += Swapped;

            if (!IsServer || contents.Length == 0) return;

            foreach (Entry entry in contents)
            {
                if (entry.Spec == null) continue;

                Add(new Item(entry.Spec.Id, entry.Amount, entry.State));
            }

            Log.Info("Entity {} starts with {} kinds of things in the bag", name, contents.Length);
        }

        public override void OnNetworkDespawn()
        {
            slots.OnListChanged -= Shifted;
            equipped.OnValueChanged -= Swapped;
        }

        public Item At(int slot)
        {
            return slots[slot];
        }

        public ItemSpec Spec(Item item)
        {
            return Catalog == null ? null : Catalog.Spec(item.Id);
        }

        public bool Equipable(Item item)
        {
            ItemSpec spec = Spec(item);

            return spec != null && spec.Equipable;
        }

        public void Add(Item item)
        {
            if (!IsServer || item.Empty) return;

            ItemSpec spec = Spec(item);

            if (spec != null && spec.Stackable)
            {
                for (int slot = 0; slot < slots.Count; slot++)
                {
                    Item stack = slots[slot];
                    if (stack.Id != item.Id) continue;

                    slots[slot] = new Item(stack.Id, stack.Amount + item.Amount, stack.State);
                    return;
                }
            }

            slots.Add(item);
            Log.Info("Entity {} took {} of {}", name, item.Amount, item.Id);
        }

        public int Amount(FixedString32Bytes id)
        {
            int total = 0;

            foreach (Item item in slots)
            {
                if (item.Id == id) total += item.Amount;
            }

            return total;
        }

        public int Remove(FixedString32Bytes id, int amount, InventoryOnConflict onConflict)
        {
            if (!IsServer || amount <= 0) return 0;

            int available = Amount(id);
            if (onConflict == InventoryOnConflict.Rollback && available < amount) return 0;

            int left = Math.Min(available, amount);
            int removed = left;

            for (int slot = slots.Count - 1; slot >= 0 && left > 0; slot--)
            {
                Item item = slots[slot];
                if (item.Id != id) continue;

                int taken = Math.Min(item.Amount, left);
                left -= taken;

                if (item.Amount == taken) Drop(slot);
                else slots[slot] = new Item(item.Id, item.Amount - taken, item.State);
            }

            return removed;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void EquipRpc(int slot)
        {
            TryEquip(slot);
        }

        public bool TryEquip(int slot)
        {
            if (!IsServer || slot < Nothing || slot >= slots.Count) return false;

            if (slot != Nothing && !Equipable(slots[slot]))
            {
                Log.Info("Entity {} can not put {} in hands", name, slots[slot].Id);
                return false;
            }

            equipped.Value = slot;
            Log.Info("Entity {} holds slot {}", name, slot);
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

        public string Digest(DigestionDetail detail)
        {
            StringBuilder digest = new StringBuilder();

            if (Equipped(out Item item))
            {
                ItemSpec equippedSpec = Spec(item);
                digest.Append("Holding: ")
                    .Append(equippedSpec == null ? item.Id.ToString() : equippedSpec.PromptName);
            }

            if (detail == DigestionDetail.Full && slots.Count > 0)
            {
                if (digest.Length > 0) digest.Append("\n");

                digest.Append("Inventory:");

                for (int slot = 0; slot < slots.Count; slot++)
                {
                    Item carried = slots[slot];
                    ItemSpec carriedSpec = Spec(carried);

                    digest.Append("\n")
                        .Append(carriedSpec == null ? carried.Id.ToString() : carriedSpec.PromptName)
                        .Append(" x ")
                        .Append(carried.Amount);
                }
            }

            return digest.Length == 0 ? null : digest.ToString();
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        private void OnValidate()
        {
            if (contents == null) return;

            foreach (Entry entry in contents)
            {
                if (entry.Spec == null) Log.Error("Entity {} has a starting inventory slot without an item spec", name);
            }
        }

        private void Shifted(NetworkListEvent<Item> change)
        {
            Changed?.Invoke();
        }

        private void Swapped(int previous, int current)
        {
            Changed?.Invoke();
        }

        private void Drop(int slot)
        {
            slots.RemoveAt(slot);

            if (equipped.Value == slot) equipped.Value = Nothing;
            else if (equipped.Value > slot) equipped.Value--;
        }

        [Serializable]
        private struct Entry
        {
            public ItemSpec Spec;
            public int Amount;
            public int State;
        }
    }
}
