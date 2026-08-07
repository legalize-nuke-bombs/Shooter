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

        public const ulong Nothing = 0;

        [SerializeField] private ItemCatalog catalog;

        [SerializeField] private Entry[] contents;

        private readonly NetworkList<StackRecord> stackRecords = new NetworkList<StackRecord>(
            null, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private readonly NetworkList<FixedString4096Bytes> uniqueRecords = new NetworkList<FixedString4096Bytes>(
            null, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<ulong> equipped = new NetworkVariable<ulong>(
            Nothing, NetworkVariableReadPermission.Owner);

        private readonly NetworkVariable<FixedString32Bytes> holding = new NetworkVariable<FixedString32Bytes>();

        private readonly Dictionary<FixedString32Bytes, int> stacks = new Dictionary<FixedString32Bytes, int>();

        private readonly List<UniqueItem> owned = new List<UniqueItem>();

        private readonly List<UniqueItem> mirrored = new List<UniqueItem>();

        public ItemCatalog Catalog => catalog != null
            ? catalog
            : Environment.Current == null ? null : Environment.Current.Items;

        public event Action Changed;

        public IReadOnlyList<UniqueItem> Uniques => IsServer ? owned : mirrored;

        public IReadOnlyList<StackRecord> Stacks
        {
            get
            {
                var records = new List<StackRecord>(stackRecords.Count);

                foreach (StackRecord record in stackRecords) records.Add(record);

                return records;
            }
        }

        public ItemSpec Holding => Spec(holding.Value);

        public ulong EquippedId => equipped.Value;

        public int Count => stackRecords.Count + uniqueRecords.Count;

        public override void OnNetworkSpawn()
        {
            stackRecords.OnListChanged += StacksShifted;
            uniqueRecords.OnListChanged += UniquesShifted;
            equipped.OnValueChanged += Reequipped;
            holding.OnValueChanged += Rehanded;

            if (!IsServer)
            {
                Remirror();
                enabled = false;
                return;
            }

            foreach (Entry entry in contents)
            {
                if (entry.Spec == null) continue;

                Fill(entry);
            }

            Log.Info("Entity {} starts with {} kinds of things in the bag", name, contents.Length);
        }

        public override void OnNetworkDespawn()
        {
            stackRecords.OnListChanged -= StacksShifted;
            uniqueRecords.OnListChanged -= UniquesShifted;
            equipped.OnValueChanged -= Reequipped;
            holding.OnValueChanged -= Rehanded;
        }

        public ItemSpec Spec(FixedString32Bytes specId)
        {
            return Catalog == null || specId.IsEmpty ? null : Catalog.Spec(specId);
        }

        public ItemSpec Spec(UniqueItem item)
        {
            return item == null ? null : Spec(new FixedString32Bytes(item.SpecId));
        }

        public void Add(ItemSpec spec, int amount)
        {
            if (!IsServer || spec == null || amount <= 0) return;

            if (!spec.Stackable)
            {
                for (int made = 0; made < amount; made++) Create(spec);
                return;
            }

            FixedString32Bytes specId = spec.Id;
            stacks.TryGetValue(specId, out int held);
            stacks[specId] = held + amount;

            Restack(specId);
            Log.Info("Entity {} took {} of {}", name, amount, specId);
        }

        public UniqueItem Create(ItemSpec spec)
        {
            if (!IsServer || spec == null || spec.Stackable) return null;

            UniqueItemIdProvider ids = Environment.Current == null ? null : Environment.Current.ItemIds;

            if (ids == null)
            {
                Log.Error("Entity {} can not create {}: the world has no item id provider", name, spec.Key);
                return null;
            }

            UniqueItem item = spec.Create(ids.Next());
            Put(item);

            return item;
        }

        public void Put(UniqueItem item)
        {
            if (!IsServer || item == null) return;

            owned.Add(item);
            item.Clean();
            uniqueRecords.Add(Pack(item));

            Log.Info("Entity {} took {} number {}", name, item.SpecId, item.Id);
        }

        public UniqueItem Find(ulong id)
        {
            foreach (UniqueItem item in Uniques)
            {
                if (item.Id == id) return item;
            }

            return null;
        }

        public UniqueItem Take(ulong id)
        {
            if (!IsServer) return null;

            for (int index = 0; index < owned.Count; index++)
            {
                if (owned[index].Id != id) continue;

                UniqueItem item = owned[index];

                owned.RemoveAt(index);
                uniqueRecords.RemoveAt(index);

                if (equipped.Value == id) Equip(Nothing);

                return item;
            }

            return null;
        }

        public int Amount(FixedString32Bytes specId)
        {
            if (IsServer) return stacks.TryGetValue(specId, out int held) ? held : 0;

            foreach (StackRecord record in stackRecords)
            {
                if (record.SpecId == specId) return record.Amount;
            }

            return 0;
        }

        public int Remove(FixedString32Bytes specId, int amount, InventoryOnConflict onConflict)
        {
            if (!IsServer || amount <= 0) return 0;

            int available = Amount(specId);
            if (onConflict == InventoryOnConflict.Rollback && available < amount) return 0;

            int taken = Math.Min(available, amount);
            if (taken == 0) return 0;

            if (available == taken) stacks.Remove(specId);
            else stacks[specId] = available - taken;

            Restack(specId);

            return taken;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void EquipRpc(ulong id)
        {
            Equip(id);
        }

        public bool Equip(ulong id)
        {
            if (!IsServer) return false;

            if (id == Nothing)
            {
                equipped.Value = Nothing;
                holding.Value = default;

                return true;
            }

            UniqueItem item = Find(id);
            if (item == null) return false;

            ItemSpec spec = Spec(item);

            if (spec == null || !spec.Equipable)
            {
                Log.Info("Entity {} can not put {} in hands", name, item.SpecId);
                return false;
            }

            equipped.Value = id;
            holding.Value = spec.Id;

            Log.Info("Entity {} holds {} number {}", name, item.SpecId, item.Id);

            return true;
        }

        public UniqueItem Equipped()
        {
            return equipped.Value == Nothing ? null : Find(equipped.Value);
        }

        public void DrainInto(Inventory target)
        {
            if (!IsServer) return;

            foreach (KeyValuePair<FixedString32Bytes, int> stack in stacks)
                target.Add(Spec(stack.Key), stack.Value);

            foreach (UniqueItem item in owned)
                target.Put(item);

            Clear();
        }

        public void Clear()
        {
            if (!IsServer) return;

            stacks.Clear();
            owned.Clear();
            stackRecords.Clear();
            uniqueRecords.Clear();

            Equip(Nothing);
        }

        public string Digest(DigestionDetail detail)
        {
            StringBuilder digest = new StringBuilder();
            ItemSpec held = Holding;

            if (held != null) digest.Append("Holding: ").Append(held.PromptName);

            if (detail != DigestionDetail.Full) return digest.Length == 0 ? null : digest.ToString();

            foreach (StackRecord record in stackRecords)
            {
                ItemSpec spec = Spec(record.SpecId);

                Line(digest).Append(spec == null ? record.SpecId.ToString() : spec.PromptName)
                    .Append(" x ")
                    .Append(record.Amount);
            }

            foreach (UniqueItem item in Uniques)
            {
                ItemSpec spec = Spec(item);

                Line(digest).Append(spec == null ? item.SpecId : spec.PromptName);
            }

            return digest.Length == 0 ? null : digest.ToString();
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        private void LateUpdate()
        {
            for (int index = 0; index < owned.Count; index++)
            {
                UniqueItem item = owned[index];
                if (!item.Dirty) continue;

                item.Clean();
                uniqueRecords[index] = Pack(item);
            }
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
                UniqueItem item = Create(entry.Spec);

                if (item != null && entry.Equip && equipped.Value == Nothing) Equip(item.Id);
            }
        }

        private void Restack(FixedString32Bytes specId)
        {
            for (int index = 0; index < stackRecords.Count; index++)
            {
                if (stackRecords[index].SpecId != specId) continue;

                if (stacks.TryGetValue(specId, out int held)) stackRecords[index] = new StackRecord(specId, held);
                else stackRecords.RemoveAt(index);

                return;
            }

            if (stacks.TryGetValue(specId, out int added)) stackRecords.Add(new StackRecord(specId, added));
        }

        private FixedString4096Bytes Pack(UniqueItem item)
        {
            string state = JsonConvert.SerializeObject(item);
            int size = Encoding.UTF8.GetByteCount(state);

            if (size <= FixedString4096Bytes.UTF8MaxLengthInBytes) return new FixedString4096Bytes(state);

            Log.Error("Entity {} holds {} number {} whose state takes {} bytes, more than the {} the network format holds",
                name, item.SpecId, item.Id, size, FixedString4096Bytes.UTF8MaxLengthInBytes);

            return default;
        }

        private UniqueItem Unpack(FixedString4096Bytes state)
        {
            if (state.IsEmpty) return null;

            string json = state.ToString();
            JObject parsed = JObject.Parse(json);
            string specId = parsed.Value<string>(nameof(UniqueItem.SpecId));
            ItemSpec spec = Spec(new FixedString32Bytes(specId));

            if (spec == null)
            {
                Log.Error("Entity {} received a thing of unknown kind {}", name, specId);
                return null;
            }

            UniqueItem item = spec.Create(parsed.Value<ulong>(nameof(UniqueItem.Id)));
            JsonConvert.PopulateObject(json, item);
            item.Clean();

            return item;
        }

        private void Remirror()
        {
            mirrored.Clear();

            foreach (FixedString4096Bytes state in uniqueRecords)
            {
                UniqueItem item = Unpack(state);

                if (item != null) mirrored.Add(item);
            }
        }

        private static StringBuilder Line(StringBuilder digest)
        {
            return digest.Length == 0 ? digest : digest.Append("\n");
        }

        private void OnValidate()
        {
            if (contents == null) return;

            foreach (Entry entry in contents)
            {
                if (entry.Spec == null) Log.Error("Entity {} has a starting inventory slot without an item spec", name);
            }
        }

        private void StacksShifted(NetworkListEvent<StackRecord> change)
        {
            Changed?.Invoke();
        }

        private void UniquesShifted(NetworkListEvent<FixedString4096Bytes> change)
        {
            if (!IsServer) Remirror();

            Changed?.Invoke();
        }

        private void Reequipped(ulong previous, ulong current)
        {
            Changed?.Invoke();
        }

        private void Rehanded(FixedString32Bytes previous, FixedString32Bytes current)
        {
            Changed?.Invoke();
        }

        [Serializable]
        private struct Entry
        {
            public ItemSpec Spec;
            public int Amount;
            public bool Equip;
        }
    }
}
