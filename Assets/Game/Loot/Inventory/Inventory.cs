using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Loot
{
    public class Inventory : NetworkBehaviour, IDigestible
    {
        public const int NoSlot = -1;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Entry[] contents;

        private readonly NetworkVariable<int> equippedSlot = new(NoSlot);

        private readonly NetworkVariable<ItemSlots> slots = new(new ItemSlots());

        private readonly NetworkList<int> stackAmounts = new(
            null, NetworkVariableReadPermission.Owner);

        public IReadOnlyList<UniqueItem> UniqueItems => slots.Value.All;

        public int EquippedSlot => equippedSlot.Value;

        public ItemSpec EquippedSpec
        {
            get
            {
                UniqueItem item = Equipped();

                return item == null || Catalog == null ? null : Catalog.Spec(item.SpecId);
            }
        }

        private static ItemCatalog Catalog => Catalogs.Of<ItemCatalog>();

        private void Awake()
        {
            enabled = false;
        }

        private void LateUpdate()
        {
            if (!slots.Value.Dirty) return;

            slots.Value.Settle();

            SlotsShifted();
        }

        private void OnValidate()
        {
            if (contents == null) return;

            foreach (Entry entry in contents)
                if (entry.Spec == null)
                    Log.Error($"Entity {this.NameOf()} has a starting inventory slot without an item spec");
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        public string Digest(DigestionDetail detail)
        {
            var digest = new StringBuilder();
            ItemSpec held = EquippedSpec;

            if (held != null) digest.Append("Holding: ").Append(held.Id).Append(" (slot " + EquippedSlot + ")");

            if (detail != DigestionDetail.Full) return digest.Length == 0 ? null : digest.ToString();

            ItemCatalog catalog = Catalog;

            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                ItemSpec spec = catalog == null ? null : catalog.At(index);

                Line(digest).Append(spec == null ? "unknown" : spec.Id)
                    .Append(" x ")
                    .Append(stackAmounts[index]);
            }

            for (int index = 0; index < slots.Value.Count; index++)
            {
                UniqueItem item = slots.Value.At(index);
                if (item == null) continue;

                ItemSpec spec = catalog == null ? null : catalog.Spec(item.SpecId);

                Line(digest).Append((spec == null ? item.SpecId : spec.Id) + " (slot " + index + ")");
            }

            return digest.Length == 0 ? null : digest.ToString();
        }

        public event Action Changed;

        public override void OnNetworkSpawn()
        {
            stackAmounts.OnListChanged += StackAmountsShifted;
            slots.OnValueChanged += SlotsArrived;
            equippedSlot.OnValueChanged += Reequipped;

            if (!IsServer) return;

            enabled = true;

            ItemCatalog catalog = Catalog;
            int kinds = catalog == null ? 0 : catalog.Count;
            for (int index = 0; index < kinds; index++) stackAmounts.Add(0);

            foreach (Entry entry in contents)
            {
                if (entry.Spec == null) continue;

                Fill(entry);
            }

            Log.Info($"Entity {this.NameOf()} starts with {contents.Length} kinds of things in the bag");
        }

        public override void OnNetworkDespawn()
        {
            stackAmounts.OnListChanged -= StackAmountsShifted;
            slots.OnValueChanged -= SlotsArrived;
            equippedSlot.OnValueChanged -= Reequipped;

            enabled = false;
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
                Log.Error($"Entity {this.NameOf()} can not take {spec.Key}: the world catalog does not know it");
                return;
            }

            stackAmounts[index] += amount;
            Log.Info($"Entity {this.NameOf()} took {amount} of {spec.Key}");
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

            int slot = slots.Value.Put(item);

            SlotsShifted();

            Log.Info($"Entity {this.NameOf()} took {item.SpecId} into slot {slot}");

            return slot;
        }

        public UniqueItem Take(int slot)
        {
            if (!IsServer) return null;

            UniqueItem item = slots.Value.Take(slot);
            if (item == null) return null;

            if (equippedSlot.Value == slot) Equip(NoSlot);

            SlotsShifted();

            return item;
        }

        public bool Contains(UniqueItem item)
        {
            return slots.Value.Contains(item);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void EquipRpc(int slot)
        {
            Equip(slot);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void UseStackableRpc(FixedString32Bytes stackableId)
        {
            if (!UseStackable(stackableId))
                Log.Info($"Entity {this.NameOf()} failed to use stackable {stackableId} rpc");
        }

        public bool UseStackable(FixedString32Bytes stackableId)
        {
            if (Catalog.Of(stackableId) is not StackableItemSpec spec || !spec.Usable) return false;

            if (RemoveStackable(spec, 1, InventoryOnConflict.Rollback) == 0) return false;

            foreach (ItemEffect effect in spec.Effects)
                if (effect != null)
                    effect.Apply(gameObject);

            if (spec.UseSound != null && TryGetComponent(out Speaker speaker)) speaker.Play(spec.UseSound);

            Log.Info($"Entity {this.NameOf()} used one {spec.Key}");
            return true;
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
                if (stackAmounts[index] > 0 && catalog.At(index) is StackableItemSpec stackable)
                    target.AddStackable(stackable, stackAmounts[index]);

            foreach (UniqueItem item in slots.Value.All)
                if (item != null)
                    target.Put(item);

            Clear();
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
                Log.Info($"Entity {this.NameOf()} can not put {item.SpecId} in hands");
                return false;
            }

            equippedSlot.Value = slot;

            Log.Info($"Entity {this.NameOf()} holds {item.SpecId} from slot {slot}");

            return true;
        }

        private UniqueItem At(int slot)
        {
            return slots.Value.At(slot);
        }

        private void Clear()
        {
            for (int index = 0; index < stackAmounts.Count; index++)
                if (stackAmounts[index] != 0)
                    stackAmounts[index] = 0;

            slots.Value.Clear();

            SlotsShifted();

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

        private static StringBuilder Line(StringBuilder digest)
        {
            return digest.Length == 0 ? digest : digest.Append("\n");
        }

        private void StackAmountsShifted(NetworkListEvent<int> change)
        {
            Changed?.Invoke();
        }

        private void SlotsArrived(ItemSlots previous, ItemSlots current)
        {
            Changed?.Invoke();
        }

        private void SlotsShifted()
        {
            Changed?.Invoke();
        }

        private void Reequipped(int previous, int current)
        {
            Changed?.Invoke();
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
