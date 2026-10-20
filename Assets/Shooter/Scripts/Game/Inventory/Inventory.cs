using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public class Inventory : ShooterNetworkBehaviour, IDigestible, ISaveableComponent
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
            public string AssetId { get; set; }
            public SaveToken State { get; set; }
        }
        public object SaveObject()
        {
            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();

            var stacks = new Dictionary<string, int>();
            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                stacks[catalog.GetAssetAt(index).Id] = stackAmounts[index];
            }

            var savedSlots = new List<SlotData>();
            foreach (UniqueItem item in slots.ItemsBySlot)
            {
                if (item == null)
                {
                    savedSlots.Add(default);
                    continue;
                }

                object state = item.SaveObject();

                savedSlots.Add(new SlotData
                {
                    AssetId = item.AssetId,
                    State = state == null ? default : SaveToken.FromObject(state)
                });
            }

            return new SaveData { Stacks = stacks, Slots = savedSlots };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();

            RemoveAllItems();

            foreach (KeyValuePair<string, int> stack in saveData.Stacks)
            {
                if (catalog.GetAssetById(stack.Key) is not StackableItemAsset asset)
                {
                    Log.Warn($"Entity {name} lost {stack.Value} of {stack.Key}: the world catalog does not know it");
                    continue;
                }

                AddStackable(asset, stack.Value);
            }

            for (int slot = 0; slot < saveData.Slots.Count; slot++)
            {
                SlotData savedSlot = saveData.Slots[slot];
                if (savedSlot.AssetId == null) continue;

                if (catalog.GetAssetById(savedSlot.AssetId) is not UniqueItemAsset asset)
                {
                    Log.Warn($"Entity {name} lost {savedSlot.AssetId}: the world catalog does not know it");
                    continue;
                }

                UniqueItem item = asset.CreateItem();
                if (!savedSlot.State.IsEmpty) item.LoadObject(savedSlot.State);

                slots.PutInSlot(slot, item);
            }
        }

        public IReadOnlyList<UniqueItem> UniqueItems => slots.ItemsBySlot;

        public DigestionPriority Priority => DigestionPriority.Low;

        public string Digest(DigestionDetail detail)
        {
            if (detail != DigestionDetail.Full) return null;

            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();
            var lines = new List<string>();

            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                lines.Add($"{catalog.GetAssetAt(index).Id} x {stackAmounts[index]}");
            }

            for (int slot = 0; slot < slots.ItemsBySlot.Count; slot++)
            {
                UniqueItem item = slots.GetItemAt(slot);
                if (item == null) continue;

                lines.Add($"{catalog.GetAssetById(item.AssetId).Id} (slot {slot})");
            }

            return lines.Count == 0 ? null : string.Join("\n", lines);
        }

        public event Action Changed;

        public override void OnNetworkSpawn()
        {
            stackAmounts.OnListChanged += HandleStackAmountsChanged;
            slots.Changed += HandleSlotsChanged;

            if (!IsServer) return;

            for (int index = 0; index < Catalogs.GetCatalog<ItemCatalog>().Count; index++)
            {
                stackAmounts.Add(0);
            }
        }

        public override void OnNetworkDespawn()
        {
            stackAmounts.OnListChanged -= HandleStackAmountsChanged;
            slots.Changed -= HandleSlotsChanged;
        }

        public int CountStackable(StackableItemAsset asset)
        {
            int index = Catalogs.GetCatalog<ItemCatalog>().IndexOf(asset);

            return index < 0 || index >= stackAmounts.Count ? 0 : stackAmounts[index];
        }

        public void AddStackable(StackableItemAsset asset, int amount)
        {
            if (amount <= 0) return;

            int index = Catalogs.GetCatalog<ItemCatalog>().IndexOf(asset);
            if (index < 0)
            {
                Log.Error($"Entity {name} can not take {asset.Id}: the world catalog does not know it");
                return;
            }

            stackAmounts[index] += amount;
            Log.Info($"Entity {name} took {amount} of {asset.Id}");
        }

        public int RemoveStackable(StackableItemAsset asset, int amount, InventoryShortagePolicy shortagePolicy)
        {
            if (amount <= 0) return 0;

            int index = Catalogs.GetCatalog<ItemCatalog>().IndexOf(asset);
            if (index < 0) return 0;

            int available = stackAmounts[index];
            if (shortagePolicy == InventoryShortagePolicy.Rollback && available < amount) return 0;

            int taken = Math.Min(available, amount);
            if (taken == 0) return 0;

            stackAmounts[index] = available - taken;

            return taken;
        }

        public int AddUnique(UniqueItem item)
        {
            int slot = slots.PutInFreeSlot(item);

            Log.Info($"Entity {name} took {item.AssetId} into slot {slot}");

            return slot;
        }

        public UniqueItem RemoveUniqueAt(int slot)
        {
            return slots.TakeFromSlot(slot);
        }

        public UniqueItem GetUniqueAt(int slot)
        {
            return slots.GetItemAt(slot);
        }

        public bool ContainsUnique(UniqueItem item)
        {
            return slots.ContainsItem(item);
        }

        public void DrainInto(Inventory target)
        {
            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();

            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] == 0) continue;

                target.AddStackable((StackableItemAsset)catalog.GetAssetAt(index), stackAmounts[index]);
            }

            foreach (UniqueItem item in slots.ItemsBySlot)
            {
                if (item == null) continue;

                target.AddUnique(item);
            }

            RemoveAllItems();
        }

        private void RemoveAllItems()
        {
            for (int index = 0; index < stackAmounts.Count; index++)
            {
                stackAmounts[index] = 0;
            }

            slots.RemoveAllItems();
        }

        private void HandleStackAmountsChanged(NetworkListEvent<int> change)
        {
            Changed?.Invoke();
        }

        private void HandleSlotsChanged()
        {
            Changed?.Invoke();
        }
    }
}
