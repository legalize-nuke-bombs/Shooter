using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public class Inventory : NetworkBehaviour, IDigestible, ISaveableComponent
    {
        public const int NoSlot = -1;
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkList<int> stackAmounts = new(null, NetworkVariableReadPermission.Owner);

        private readonly NetworkItemSlots slots = new();

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
            ItemCatalog catalog = Catalogs.Of<ItemCatalog>();

            var stacks = new Dictionary<string, int>();
            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                stacks[catalog.At(index).Key] = stackAmounts[index];
            }

            var saved = new List<SlotData>();
            foreach (UniqueItem item in slots.All)
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
            ItemCatalog catalog = Catalogs.Of<ItemCatalog>();

            Clear();

            foreach (KeyValuePair<string, int> stack in sd.Stacks)
            {
                if (catalog.Spec(stack.Key) is not StackableItemSpec spec)
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

                if (catalog.Spec(saved.SpecId) is not UniqueItemSpec spec)
                {
                    Log.Warn($"Entity {name} lost {saved.SpecId}: the world catalog does not know it");
                    continue;
                }

                UniqueItem item = spec.Create();
                if (!saved.State.Empty) item.LoadObject(saved.State);

                slots.Put(slot, item);
            }
        }

        public IReadOnlyList<UniqueItem> UniqueItems => slots.All;

        public DigestionPriority Priority => DigestionPriority.Low;

        public string Digest(DigestionDetail detail)
        {
            if (detail != DigestionDetail.Full) return null;

            ItemCatalog catalog = Catalogs.Of<ItemCatalog>();
            var lines = new List<string>();

            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                lines.Add($"{catalog.At(index).Id} x {stackAmounts[index]}");
            }

            for (int slot = 0; slot < slots.All.Count; slot++)
            {
                UniqueItem item = slots.At(slot);
                if (item == null) continue;

                lines.Add($"{catalog.Spec(item.SpecId).Id} (slot {slot})");
            }

            return lines.Count == 0 ? null : string.Join("\n", lines);
        }

        public event Action Changed;

        public override void OnNetworkSpawn()
        {
            stackAmounts.OnListChanged += StackAmountsChanged;
            slots.Changed += SlotsChanged;

            if (!IsServer) return;

            for (int index = 0; index < Catalogs.Of<ItemCatalog>().Count; index++)
            {
                stackAmounts.Add(0);
            }
        }

        public override void OnNetworkDespawn()
        {
            stackAmounts.OnListChanged -= StackAmountsChanged;
            slots.Changed -= SlotsChanged;
        }

        public int Count(StackableItemSpec spec)
        {
            int index = Catalogs.Of<ItemCatalog>().Index(spec);

            return index < 0 || index >= stackAmounts.Count ? 0 : stackAmounts[index];
        }

        public void Add(StackableItemSpec spec, int amount)
        {
            if (amount <= 0) return;

            int index = Catalogs.Of<ItemCatalog>().Index(spec);
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

            int index = Catalogs.Of<ItemCatalog>().Index(spec);
            if (index < 0) return 0;

            int available = stackAmounts[index];
            if (onConflict == InventoryOnConflict.Rollback && available < amount) return 0;

            int taken = Math.Min(available, amount);
            if (taken == 0) return 0;

            stackAmounts[index] = available - taken;

            return taken;
        }

        public int Add(UniqueItem item)
        {
            int slot = slots.Put(item);

            Log.Info($"Entity {name} took {item.SpecId} into slot {slot}");

            return slot;
        }

        public UniqueItem Remove(int slot)
        {
            return slots.Take(slot);
        }

        public UniqueItem At(int slot)
        {
            return slots.At(slot);
        }

        public bool Contains(UniqueItem item)
        {
            return slots.Contains(item);
        }

        public void DrainInto(Inventory target)
        {
            ItemCatalog catalog = Catalogs.Of<ItemCatalog>();

            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                target.Add((StackableItemSpec)catalog.At(index), stackAmounts[index]);
            }

            foreach (UniqueItem item in slots.All)
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
                stackAmounts[index] = 0;
            }

            slots.Clear();
        }

        private void StackAmountsChanged(NetworkListEvent<int> change)
        {
            Changed?.Invoke();
        }

        private void SlotsChanged()
        {
            Changed?.Invoke();
        }
    }
}
