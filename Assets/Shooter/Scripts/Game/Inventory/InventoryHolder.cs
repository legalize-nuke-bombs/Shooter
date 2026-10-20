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

        public string ComponentKey => "InventoryHolder";
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
            HoldSlot(content.ToObject<SaveData>().Slot);
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
            heldSlot.OnValueChanged += HandleHeldSlotChanged;
            inventory.Changed += RelayInventoryChanged;

            if (!IsServer) return;

            inventory.Changed += StopHoldingIfSlotEmptied;
        }

        public override void OnNetworkDespawn()
        {
            heldSlot.OnValueChanged -= HandleHeldSlotChanged;
            inventory.Changed -= RelayInventoryChanged;

            if (!IsServer) return;

            inventory.Changed -= StopHoldingIfSlotEmptied;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void HoldSlotRpc(int slot)
        {
            HoldSlot(slot);
        }

        public void HoldSlot(int slot)
        {
            if (slot == Inventory.NoSlot)
            {
                heldSlot.Value = Inventory.NoSlot;
                return;
            }

            UniqueItem item = inventory.GetUniqueAt(slot);
            if (item == null)
            {
                Log.Info($"Entity {name} can not hold slot {slot}: the slot is empty");
                return;
            }

            UniqueItemAsset asset = (UniqueItemAsset)Catalogs.GetCatalog<ItemCatalog>().GetAssetById(item.AssetId);
            if (!asset.IsEquipable)
            {
                Log.Info($"Entity {name} can not put {item.AssetId} in hands");
                return;
            }

            heldSlot.Value = slot;
            Log.Info($"Entity {name} holds {item.AssetId} from slot {slot}");
        }

        private void StopHoldingIfSlotEmptied()
        {
            if (heldSlot.Value == Inventory.NoSlot || HeldItem != null) return;

            Log.Info($"Entity {name} holds nothing now: slot {heldSlot.Value} is empty");
            heldSlot.Value = Inventory.NoSlot;
        }

        private void RelayInventoryChanged()
        {
            Changed?.Invoke();
        }

        private void HandleHeldSlotChanged(int previousSlot, int currentSlot)
        {
            Changed?.Invoke();
        }
    }
}
