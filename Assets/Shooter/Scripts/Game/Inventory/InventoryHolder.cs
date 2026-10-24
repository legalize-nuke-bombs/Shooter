using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    public class InventoryHolder : ShooterNetworkBehaviour, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<int> heldSlot = new(Inventory.NoSlot);

        private Inventory inventory;

        private struct SaveData
        {
            public int Slot { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData { Slot = heldSlot.Value };
        }
        public void LoadObject(SaveToken content)
        {
            int slot = content.ToObject<SaveData>().Slot;
            InventoryHolderTryHoldSlotResultCode result = HoldSlot(slot);
            if (result != InventoryHolderTryHoldSlotResultCode.Ok) Log.Warn($"Entity {name} can not hold saved slot {slot}: {result}");
        }

        public int HeldSlot => heldSlot.Value;

        public UniqueItem HeldItem => inventory.GetUniqueAt(heldSlot.Value);

        public UniqueItemAsset HeldAsset
        {
            get
            {
                UniqueItem item = HeldItem;

                return item == null ? null : (UniqueItemAsset)Catalogs.GetCatalog<ItemCatalog>().GetAssetById(item.AssetId);
            }
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        public string Digest(DigestionDetail detail)
        {
            UniqueItemAsset heldAsset = HeldAsset;

            return heldAsset == null ? null : $"Holding: {heldAsset.Id} (slot {heldSlot.Value})";
        }

        public event Action Changed;

        protected override void ShooterAwake()
        {
            inventory = GetComponent<Inventory>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                inventory.UniqueRemoved += HandleUniqueRemoved;
            }
            else
            {
                heldSlot.OnValueChanged += HandleHeldSlotChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                inventory.UniqueRemoved -= HandleUniqueRemoved;
            }
            else
            {
                heldSlot.OnValueChanged -= HandleHeldSlotChanged;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void HoldSlotRpc(int slot)
        {
            InventoryHolderTryHoldSlotResultCode result = HoldSlot(slot);
            Log.Info($"Entity {name} inventory holder hold slot {slot} rpc result code: {result}");
        }

        public enum InventoryHolderTryHoldSlotResultCode
        {
            SlotIsEmpty,
            SlotIsNotEquipable,
            Ok
        }
        public InventoryHolderTryHoldSlotResultCode HoldSlot(int slot)
        {
            if (slot != Inventory.NoSlot)
            {
                UniqueItem item = inventory.GetUniqueAt(slot);
                if (item == null)
                {
                    return InventoryHolderTryHoldSlotResultCode.SlotIsEmpty;
                }

                UniqueItemAsset asset = (UniqueItemAsset)Catalogs.GetCatalog<ItemCatalog>().GetAssetById(item.AssetId);
                if (!asset.IsEquipable)
                {
                    return InventoryHolderTryHoldSlotResultCode.SlotIsNotEquipable;
                }
            }

            if (heldSlot.Value == slot) return InventoryHolderTryHoldSlotResultCode.Ok;

            heldSlot.Value = slot;
            Changed?.Invoke();
            return InventoryHolderTryHoldSlotResultCode.Ok;
        }

        private void HandleUniqueRemoved(int slot, UniqueItem item)
        {
            if (slot != heldSlot.Value)
            {
                Log.Info($"Entity {name} keeps holding slot {heldSlot.Value}: {item.AssetId} left slot {slot}");
                return;
            }

            heldSlot.Value = Inventory.NoSlot;
            Log.Info($"Entity {name} holds nothing now: {item.AssetId} left slot {slot}");
            Changed?.Invoke();
        }

        private void HandleHeldSlotChanged(int previousSlot, int currentSlot)
        {
            Changed?.Invoke();
        }
    }
}
