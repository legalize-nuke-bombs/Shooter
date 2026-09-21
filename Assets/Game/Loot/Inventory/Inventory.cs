using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Loot
{
    public class Inventory : NetworkBehaviour, IDigestible, ISaveableComponent
    {
        public const int NoSlot = -1;
        private static readonly Journal Log = Logs.Here();

        [Serializable]
        private struct Entry
        {
            [SerializeField] private ItemSpec spec;
            [SerializeField] private int amount;

            public ItemSpec Spec => spec;

            public int Amount => amount;
        }

        [SerializeField] private Entry[] contents;

        private readonly NetworkVariable<ItemSlots> slots = new(new ItemSlots());

        private readonly NetworkList<int> stackAmounts = new(null, NetworkVariableReadPermission.Owner);

        public string ComponentKey => "Inventory";
        private struct SaveData
        {
            public Dictionary<string, int> Stacks { get; set; }
            public List<SlotData> Slots { get; set; }
        }
        private struct SlotData
        {
            public string SpecId { get; set; }
            public SaveToken State { get; set; }
        }
        public object SaveObject()
        {
            var stacks = new Dictionary<string, int>();
            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                stacks[Catalog.At(index).Key] = stackAmounts[index];
            }

            var saved = new List<SlotData>();
            foreach (UniqueItem item in slots.Value.All)
            {
                if (item == null)
                {
                    saved.Add(default);
                    continue;
                }

                object state = item.SaveObject();

                saved.Add(new SlotData
                {
                    SpecId = item.SpecId,
                    State = state == null ? default : SaveToken.From(state)
                });
            }

            return new SaveData { Stacks = stacks, Slots = saved };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();

            Clear();

            foreach (KeyValuePair<string, int> stack in sd.Stacks)
            {
                if (Catalog.Spec(stack.Key) is not StackableItemSpec spec)
                {
                    Log.Warn($"Entity {name} lost {stack.Value} of {stack.Key}: the world catalog does not know it");
                    continue;
                }

                Add(spec, stack.Value);
            }

            for (int slot = 0; slot < sd.Slots.Count; slot++)
            {
                SlotData saved = sd.Slots[slot];
                if (saved.SpecId == null) continue;

                if (Catalog.Spec(saved.SpecId) is not UniqueItemSpec spec)
                {
                    Log.Warn($"Entity {name} lost {saved.SpecId}: the world catalog does not know it");
                    continue;
                }

                UniqueItem item = spec.Create();
                if (!saved.State.Empty) item.LoadObject(saved.State);

                slots.Value.Put(slot, item);
            }

            SlotsShifted();
        }

        public IReadOnlyList<UniqueItem> UniqueItems => slots.Value.All;

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
            foreach (Entry entry in contents)
            {
                if (entry.Spec != null) continue;

                Log.Error($"Entity {name} has a starting inventory slot without an item spec");
            }
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        public string Digest(DigestionDetail detail)
        {
            if (detail != DigestionDetail.Full) return null;

            var digest = new StringBuilder();

            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                Line(digest).Append(Catalog.At(index).Id).Append(" x ").Append(stackAmounts[index]);
            }

            for (int slot = 0; slot < slots.Value.Count; slot++)
            {
                UniqueItem item = slots.Value.At(slot);
                if (item == null) continue;

                Line(digest).Append(Catalog.Spec(item.SpecId).Id).Append(" (slot " + slot + ")");
            }

            return digest.Length == 0 ? null : digest.ToString();
        }

        public event Action Changed;

        public override void OnNetworkSpawn()
        {
            stackAmounts.OnListChanged += StackAmountsShifted;
            slots.OnValueChanged += SlotsArrived;

            if (!IsServer) return;

            enabled = true;

            for (int index = 0; index < Catalog.Count; index++)
            {
                stackAmounts.Add(0);
            }

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
            slots.OnValueChanged -= SlotsArrived;

            enabled = false;
        }

        public int Count(StackableItemSpec spec)
        {
            int index = Catalog.Index(spec);

            return index < 0 || index >= stackAmounts.Count ? 0 : stackAmounts[index];
        }

        public void Add(StackableItemSpec spec, int amount)
        {
            if (amount <= 0) return;

            int index = Catalog.Index(spec);
            if (index < 0)
            {
                Log.Error($"Entity {name} can not take {spec.Key}: the world catalog does not know it");
                return;
            }

            stackAmounts[index] += amount;
            Log.Info($"Entity {name} took {amount} of {spec.Key}");
        }

        public int Remove(StackableItemSpec spec, int amount, InventoryOnConflict onConflict)
        {
            if (amount <= 0) return 0;

            int index = Catalog.Index(spec);
            if (index < 0) return 0;

            int available = stackAmounts[index];
            if (onConflict == InventoryOnConflict.Rollback && available < amount) return 0;

            int taken = Math.Min(available, amount);
            if (taken == 0) return 0;

            stackAmounts[index] = available - taken;

            return taken;
        }

        public void Add(UniqueItem item)
        {
            int slot = slots.Value.Put(item);

            SlotsShifted();

            Log.Info($"Entity {name} took {item.SpecId} into slot {slot}");
        }

        public UniqueItem Remove(int slot)
        {
            UniqueItem item = slots.Value.Take(slot);
            if (item == null) return null;

            SlotsShifted();

            return item;
        }

        public UniqueItem At(int slot)
        {
            return slots.Value.At(slot);
        }

        public bool Contains(UniqueItem item)
        {
            return slots.Value.Contains(item);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void UseRpc(FixedString32Bytes stackableId)
        {
            if (Catalog.Of(stackableId) is StackableItemSpec spec && Use(spec)) return;

            Log.Info($"Entity {name} failed to use {stackableId} rpc");
        }

        public bool Use(StackableItemSpec spec)
        {
            if (!spec.Usable) return false;

            if (Remove(spec, 1, InventoryOnConflict.Rollback) == 0) return false;

            foreach (ItemEffect effect in spec.Effects)
            {
                effect.Apply(gameObject);
            }

            Speaker speaker = GetComponent<Speaker>();
            if (speaker != null) speaker.Play(spec.UseSound);

            Log.Info($"Entity {name} used one {spec.Key}");
            return true;
        }

        public void DrainInto(Inventory target)
        {
            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                target.Add((StackableItemSpec)Catalog.At(index), stackAmounts[index]);
            }

            foreach (UniqueItem item in slots.Value.All)
            {
                if (item == null) continue;

                target.Add(item);
            }

            Clear();
        }

        private void Clear()
        {
            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] != 0) stackAmounts[index] = 0;
            }

            slots.Value.Clear();

            SlotsShifted();
        }

        private void Fill(Entry entry)
        {
            int amount = Math.Max(entry.Amount, 1);

            if (entry.Spec is StackableItemSpec stackable)
            {
                Add(stackable, amount);
                return;
            }

            var unique = (UniqueItemSpec)entry.Spec;

            for (int made = 0; made < amount; made++)
            {
                Add(unique.Create());
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

        private void SlotsArrived(ItemSlots previous, ItemSlots current)
        {
            Changed?.Invoke();
        }

        private void SlotsShifted()
        {
            Changed?.Invoke();
        }
    }
}
