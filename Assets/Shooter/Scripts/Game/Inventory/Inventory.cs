using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class Inventory : NetworkBehaviour, IDigestible, ISaveableComponent
    {
        public const int NoSlot = -1;
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkList<int> stackAmounts = new(null, NetworkVariableReadPermission.Owner);

        private readonly NetworkItemSlots slots = new();

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

                stackAmounts[catalog.IndexOf(asset)] = stack.Value;
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

            Changed?.Invoke();
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

                lines.Add($"{item.AssetId} (slot {slot})");
            }

            return lines.Count == 0 ? "No items" : string.Join("\n", lines);
        }

        public event Action Changed;

        // Raised on the server only: a client gets the slots from the network all at once and does not see what left
        public event Action<int, UniqueItem> UniqueRemoved;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                for (int index = 0; index < Catalogs.GetCatalog<ItemCatalog>().Count; index++)
                {
                    stackAmounts.Add(0);
                }
            }
            else
            {
                stackAmounts.OnListChanged += HandleStackAmountsReplicated;
                slots.Replicated += HandleSlotsReplicated;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) return;

            stackAmounts.OnListChanged -= HandleStackAmountsReplicated;
            slots.Replicated -= HandleSlotsReplicated;
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
            Changed?.Invoke();
        }

        public enum InventoryShortagePolicy
        {
            Rollback,
            Partly
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
            Changed?.Invoke();

            return taken;
        }

        public int AddUnique(UniqueItem item)
        {
            int slot = slots.PutInFreeSlot(item);
            Log.Info($"Entity {name} took {item.AssetId} into slot {slot}");
            Changed?.Invoke();

            return slot;
        }

        public UniqueItem RemoveUniqueAt(int slot)
        {
            UniqueItem item = slots.TakeFromSlot(slot);
            if (item == null) return null;

            UniqueRemoved?.Invoke(slot, item);
            Changed?.Invoke();

            return item;
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
            Changed?.Invoke();
        }

        private void RemoveAllItems()
        {
            for (int index = 0; index < stackAmounts.Count; index++)
            {
                if (stackAmounts[index] != 0) stackAmounts[index] = 0;
            }

            for (int slot = 0; slot < slots.ItemsBySlot.Count; slot++)
            {
                UniqueItem item = slots.TakeFromSlot(slot);
                if (item != null) UniqueRemoved?.Invoke(slot, item);
            }

            slots.RemoveAllItems();
        }

        private void HandleStackAmountsReplicated(NetworkListEvent<int> change)
        {
            Changed?.Invoke();
        }

        private void HandleSlotsReplicated()
        {
            Changed?.Invoke();
        }

        private class NetworkItemSlots : NetworkVariableBase
        {
            private readonly List<UniqueItem> items = new();

            public IReadOnlyList<UniqueItem> ItemsBySlot => items;

            public event Action Replicated;

            public UniqueItem GetItemAt(int slot)
            {
                return slot >= 0 && slot < items.Count ? items[slot] : null;
            }

            public bool ContainsItem(UniqueItem item)
            {
                return items.Contains(item);
            }

            public int PutInFreeSlot(UniqueItem item)
            {
                int slot = items.IndexOf(null);
                if (slot < 0) slot = items.Count;

                PutInSlot(slot, item);

                return slot;
            }

            public void PutInSlot(int slot, UniqueItem item)
            {
                while (items.Count <= slot)
                {
                    items.Add(null);
                }

                items[slot] = item;
                item.Changed += MarkDirty;

                MarkDirty();
            }

            public UniqueItem TakeFromSlot(int slot)
            {
                UniqueItem item = GetItemAt(slot);
                if (item == null) return null;

                items[slot] = null;
                item.Changed -= MarkDirty;

                MarkDirty();

                return item;
            }

            public void RemoveAllItems()
            {
                foreach (UniqueItem item in items)
                {
                    if (item == null) continue;

                    item.Changed -= MarkDirty;
                }

                items.Clear();

                MarkDirty();
            }

            public override void WriteField(FastBufferWriter writer)
            {
                ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();

                writer.WriteValueSafe(items.Count);

                foreach (UniqueItem item in items)
                {
                    writer.WriteValueSafe(catalog.GetAssetIndexOf(item));
                    if (item != null) writer.WriteNetworkSerializable(item);
                }
            }

            public override void ReadField(FastBufferReader reader)
            {
                ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();

                reader.ReadValueSafe(out int slotCount);
                items.Clear();

                for (int slot = 0; slot < slotCount; slot++)
                {
                    reader.ReadValueSafe(out int assetIndex);

                    UniqueItem item = assetIndex < 0 ? null : catalog.CreateUniqueItemAt(assetIndex);
                    if (item != null) reader.ReadNetworkSerializableInPlace(ref item);

                    items.Add(item);
                }

                Replicated?.Invoke();
            }

            public override void WriteDelta(FastBufferWriter writer)
            {
                WriteField(writer);
            }

            public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
            {
                ReadField(reader);
            }

            private void MarkDirty()
            {
                SetDirty(true);
            }
        }
    }
}
