using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Packing;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Loot
{
    public class Inventory : NetworkBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        public const int NoSlot = -1;

        [SerializeField] private Entry[] contents;

        private readonly NetworkList<int> stackAmounts = new NetworkList<int>(
            null, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private readonly NetworkList<PackedBytes> packedUniqueItems = new NetworkList<PackedBytes>();

        private readonly NetworkVariable<int> equippedSlot = new NetworkVariable<int>(NoSlot);

        private readonly List<UniqueItem> uniqueItems = new List<UniqueItem>();

        public event Action Changed;

        public IReadOnlyList<UniqueItem> UniqueItems => uniqueItems;

        public int EquippedSlot => equippedSlot.Value;

        public ItemSpec EquippedSpec
        {
            get
            {
                UniqueItem item = Equipped();

                return item == null || Catalog == null ? null : Catalog.Spec(item.SpecId);
            }
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        private static ItemCatalog Catalog => Environment.Current == null ? null : Environment.Current.Items;

        private void Awake()
        {
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            stackAmounts.OnListChanged += StackAmountsShifted;
            packedUniqueItems.OnListChanged += PackedUniqueItemsShifted;
            equippedSlot.OnValueChanged += Reequipped;

            if (!IsServer)
            {
                Remirror();
                return;
            }

            enabled = true;

            ItemCatalog catalog = Catalog;
            int kinds = catalog == null ? 0 : catalog.Count;
            for (int index = 0; index < kinds; index++) stackAmounts.Add(0);

            foreach (Entry entry in contents)
            {
                if (entry.Spec == null) continue;

                Fill(entry);
            }

            Log.Info($"Entity {name} starts with {contents.Length} kinds of things in the bag");
        }

        public override void OnNetworkDespawn()
        {
            stackAmounts.OnListChanged -= StackAmountsShifted;
            packedUniqueItems.OnListChanged -= PackedUniqueItemsShifted;
            equippedSlot.OnValueChanged -= Reequipped;

            enabled = false;
        }

        private void LateUpdate()
        {
            for (int slot = 0; slot < uniqueItems.Count; slot++)
            {
                UniqueItem item = uniqueItems[slot];
                if (item == null || !item.Dirty) continue;

                item.Clean();
                packedUniqueItems[slot] = PackedBytes.Of<UniqueItem>(item);
            }
        }

        public int StackableAmount(StackableItemSpec spec)
        {
            int index = IndexOf(spec);

            return index < 0 || index >= stackAmounts.Count ? 0 : stackAmounts[index];
        }

        public void AddStackable(StackableItemSpec spec, int amount)
        {
            if (!IsServer || amount <= 0 || spec == null) return;

            int index = IndexOf(spec);

            if (index < 0 || index >= stackAmounts.Count)
            {
                Log.Error($"Entity {name} can not take {spec.Key}: the world catalog does not know it");
                return;
            }

            stackAmounts[index] += amount;
            Log.Info($"Entity {name} took {amount} of {spec.Key}");
        }

        public int RemoveStackable(StackableItemSpec spec, int amount, InventoryOnConflict onConflict)
        {
            if (!IsServer || amount <= 0 || spec == null) return 0;

            int index = IndexOf(spec);
            if (index < 0 || index >= stackAmounts.Count) return 0;

            int available = stackAmounts[index];
            if (onConflict == InventoryOnConflict.Rollback && available < amount) return 0;

            int taken = Math.Min(available, amount);
            if (taken == 0) return 0;

            stackAmounts[index] = available - taken;

            return taken;
        }

        public int Put(UniqueItem item)
        {
            if (!IsServer || item == null) return NoSlot;

            item.Clean();
            int slot = uniqueItems.IndexOf(null);

            if (slot == NoSlot)
            {
                slot = uniqueItems.Count;
                uniqueItems.Add(item);
                packedUniqueItems.Add(PackedBytes.Of<UniqueItem>(item));
            }
            else
            {
                uniqueItems[slot] = item;
                packedUniqueItems[slot] = PackedBytes.Of<UniqueItem>(item);
            }

            Log.Info($"Entity {name} took {item.SpecId} into slot {slot}");

            return slot;
        }

        public UniqueItem Take(int slot)
        {
            if (!IsServer) return null;

            UniqueItem item = At(slot);
            if (item == null) return null;

            uniqueItems[slot] = null;
            packedUniqueItems[slot] = default;

            if (equippedSlot.Value == slot) Equip(NoSlot);

            return item;
        }

        public bool Contains(UniqueItem item)
        {
            return item != null && uniqueItems.Contains(item);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void EquipRpc(int slot)
        {
            Equip(slot);
        }

        public UniqueItem Equipped()
        {
            return At(equippedSlot.Value);
        }

        public void DrainInto(Inventory target)
        {
            if (!IsServer) return;

            ItemCatalog catalog = Catalog;

            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] > 0 && catalog.At(index) is StackableItemSpec stackable)
                    target.AddStackable(stackable, stackAmounts[index]);
            }

            foreach (UniqueItem item in uniqueItems)
            {
                if (item != null) target.Put(item);
            }

            Clear();
        }

        public string Digest(DigestionDetail detail)
        {
            StringBuilder digest = new StringBuilder();
            ItemSpec held = EquippedSpec;

            if (held != null) digest.Append("Holding: ").Append(held.PromptName).Append(" (slot " + EquippedSlot + ")");

            if (detail != DigestionDetail.Full) return digest.Length == 0 ? null : digest.ToString();

            ItemCatalog catalog = Catalog;

            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                ItemSpec spec = catalog == null ? null : catalog.At(index);

                Line(digest).Append(spec == null ? "unknown" : spec.PromptName)
                    .Append(" x ")
                    .Append(stackAmounts[index]);
            }

            for (int index = 0; index < uniqueItems.Count; index++)
            {
                UniqueItem item = uniqueItems[index];
                if (item == null) continue;

                ItemSpec spec = catalog == null ? null : catalog.Spec(item.SpecId);

                Line(digest).Append((spec == null ? item.SpecId : spec.PromptName) + " (slot " + index + ")");
            }

            return digest.Length == 0 ? null : digest.ToString();
        }

        private bool Equip(int slot)
        {
            if (!IsServer) return false;

            if (slot == NoSlot)
            {
                equippedSlot.Value = NoSlot;

                return true;
            }

            UniqueItem item = At(slot);
            if (item == null) return false;

            var spec = (Catalog == null ? null : Catalog.Spec(item.SpecId)) as UniqueItemSpec;

            if (spec == null || !spec.Equipable)
            {
                Log.Info($"Entity {name} can not put {item.SpecId} in hands");
                return false;
            }

            equippedSlot.Value = slot;

            Log.Info($"Entity {name} holds {item.SpecId} from slot {slot}");

            return true;
        }

        private UniqueItem At(int slot)
        {
            return slot >= 0 && slot < uniqueItems.Count ? uniqueItems[slot] : null;
        }

        private void Clear()
        {
            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] != 0) stackAmounts[index] = 0;
            }

            uniqueItems.Clear();
            packedUniqueItems.Clear();

            Equip(NoSlot);
        }

        private void Fill(Entry entry)
        {
            int amount = Math.Max(entry.Amount, 1);

            if (entry.Spec is StackableItemSpec stackable)
            {
                AddStackable(stackable, amount);
                return;
            }

            if (entry.Spec is not UniqueItemSpec unique) return;

            for (int made = 0; made < amount; made++)
            {
                int slot = Put(unique.Create());

                if (slot != NoSlot && entry.Equip && equippedSlot.Value == NoSlot) Equip(slot);
            }
        }

        private static int IndexOf(ItemSpec spec)
        {
            ItemCatalog catalog = Catalog;

            return catalog == null || spec == null ? -1 : catalog.Index(spec);
        }

        private void Remirror()
        {
            uniqueItems.Clear();

            foreach (PackedBytes state in packedUniqueItems) uniqueItems.Add(state.Unpack<UniqueItem>());
        }

        private void Mirror(NetworkListEvent<PackedBytes> change)
        {
            switch (change.Type)
            {
                case NetworkListEvent<PackedBytes>.EventType.Add:
                    uniqueItems.Add(change.Value.Unpack<UniqueItem>());
                    break;
                case NetworkListEvent<PackedBytes>.EventType.Value:
                    uniqueItems[change.Index] = change.Value.Unpack<UniqueItem>();
                    break;
                case NetworkListEvent<PackedBytes>.EventType.Clear:
                    uniqueItems.Clear();
                    break;
                default:
                    Remirror();
                    break;
            }
        }

        private static StringBuilder Line(StringBuilder digest)
        {
            return digest.Length == 0 ? digest : digest.Append("\n");
        }

        private void StackAmountsShifted(NetworkListEvent<int> change)
        {
            Changed?.Invoke();
        }

        private void PackedUniqueItemsShifted(NetworkListEvent<PackedBytes> change)
        {
            if (!IsServer) Mirror(change);

            Changed?.Invoke();
        }

        private void Reequipped(int previous, int current)
        {
            Changed?.Invoke();
        }

        private void OnValidate()
        {
            if (contents == null) return;

            foreach (Entry entry in contents)
            {
                if (entry.Spec == null) Log.Error($"Entity {name} has a starting inventory slot without an item spec");
            }
        }

        [Serializable]
        private struct Entry
        {
            [SerializeField] private ItemSpec spec;
            [SerializeField] private int amount;
            [SerializeField] private bool equip;

            public ItemSpec Spec => spec;

            public int Amount => amount;

            public bool Equip => equip;
        }
    }
}
