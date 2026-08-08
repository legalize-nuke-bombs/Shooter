using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public const int NoSlot = -1;

        [SerializeField] private Entry[] contents;

        private readonly NetworkList<int> stackAmounts = new NetworkList<int>(
            null, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private readonly NetworkList<FixedString4096Bytes> packedUniqueItems = new NetworkList<FixedString4096Bytes>();

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

            Log.Info("Entity {} starts with {} kinds of things in the bag", name, contents.Length);
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
                packedUniqueItems[slot] = Pack(item);
            }
        }

        public int Amount(ItemSpec spec)
        {
            int index = IndexOf(spec);

            return index < 0 || index >= stackAmounts.Count ? 0 : stackAmounts[index];
        }

        public void Add(ItemSpec spec, int amount)
        {
            if (!IsServer || spec == null || amount <= 0) return;

            if (!spec.Stackable)
            {
                for (int made = 0; made < amount; made++) Put(spec.Create());
                return;
            }

            int index = IndexOf(spec);

            if (index < 0 || index >= stackAmounts.Count)
            {
                Log.Error("Entity {} can not take {}: the world catalog does not know it", name, spec.Key);
                return;
            }

            stackAmounts[index] += amount;
            Log.Info("Entity {} took {} of {}", name, amount, spec.Key);
        }

        public int Remove(ItemSpec spec, int amount, InventoryOnConflict onConflict)
        {
            if (!IsServer || amount <= 0) return 0;

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
                packedUniqueItems.Add(Pack(item));
            }
            else
            {
                uniqueItems[slot] = item;
                packedUniqueItems[slot] = Pack(item);
            }

            Log.Info("Entity {} took {} into slot {}", name, item.SpecId, slot);

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
                if (stackAmounts[index] > 0) target.Add(catalog.At(index), stackAmounts[index]);
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

            if (held != null) digest.Append("Holding: ").Append(held.PromptName);

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

            foreach (UniqueItem item in uniqueItems)
            {
                if (item == null) continue;

                ItemSpec spec = catalog == null ? null : catalog.Spec(item.SpecId);

                Line(digest).Append(spec == null ? item.SpecId : spec.PromptName);
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

            ItemSpec spec = Catalog == null ? null : Catalog.Spec(item.SpecId);

            if (spec == null || !spec.Equipable)
            {
                Log.Info("Entity {} can not put {} in hands", name, item.SpecId);
                return false;
            }

            equippedSlot.Value = slot;

            Log.Info("Entity {} holds {} from slot {}", name, item.SpecId, slot);

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

            if (entry.Spec.Stackable)
            {
                Add(entry.Spec, amount);
                return;
            }

            for (int made = 0; made < amount; made++)
            {
                int slot = Put(entry.Spec.Create());

                if (slot != NoSlot && entry.Equip && equippedSlot.Value == NoSlot) Equip(slot);
            }
        }

        private static int IndexOf(ItemSpec spec)
        {
            ItemCatalog catalog = Catalog;

            return catalog == null || spec == null ? -1 : catalog.Index(spec);
        }

        private FixedString4096Bytes Pack(UniqueItem item)
        {
            string state = JsonConvert.SerializeObject(item);
            int size = Encoding.UTF8.GetByteCount(state);

            if (size <= FixedString4096Bytes.UTF8MaxLengthInBytes) return new FixedString4096Bytes(state);

            Log.Error("Entity {} holds {} whose state takes {} bytes, more than the {} the network format holds",
                name, item.SpecId, size, FixedString4096Bytes.UTF8MaxLengthInBytes);

            return default;
        }

        private UniqueItem Unpack(FixedString4096Bytes state)
        {
            if (state.IsEmpty) return null;

            string json = state.ToString();
            JObject parsed = JObject.Parse(json);
            string specId = parsed.Value<string>(nameof(UniqueItem.SpecId));
            ItemSpec spec = Catalog == null ? null : Catalog.Spec(specId);

            if (spec == null)
            {
                Log.Error("Entity {} received a thing of unknown kind {}", name, specId);
                return null;
            }

            UniqueItem item = spec.Create();
            JsonConvert.PopulateObject(json, item);
            item.Clean();

            return item;
        }

        private void Remirror()
        {
            uniqueItems.Clear();

            foreach (FixedString4096Bytes state in packedUniqueItems) uniqueItems.Add(Unpack(state));
        }

        private void Mirror(NetworkListEvent<FixedString4096Bytes> change)
        {
            switch (change.Type)
            {
                case NetworkListEvent<FixedString4096Bytes>.EventType.Add:
                    uniqueItems.Add(Unpack(change.Value));
                    break;
                case NetworkListEvent<FixedString4096Bytes>.EventType.Value:
                    uniqueItems[change.Index] = Unpack(change.Value);
                    break;
                case NetworkListEvent<FixedString4096Bytes>.EventType.Clear:
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

        private void PackedUniqueItemsShifted(NetworkListEvent<FixedString4096Bytes> change)
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
                if (entry.Spec == null) Log.Error("Entity {} has a starting inventory slot without an item spec", name);
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
