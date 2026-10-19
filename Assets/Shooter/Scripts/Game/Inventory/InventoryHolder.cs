using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    public class InventoryHolder : NetworkBehaviour, IDigestible, ISaveableComponent
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
            Hold(content.To<SaveData>().Slot);
        }

        public int Slot => heldSlot.Value;

        public UniqueItem Held => inventory.At(heldSlot.Value);

        public UniqueItemAsset HeldAsset
        {
            get
            {
                UniqueItem item = Held;

                return item == null ? null : (UniqueItemAsset)Catalogs.Of<ItemCatalog>().Asset(item.AssetId);
            }
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        public string Digest(DigestionDetail detail)
        {
            UniqueItemAsset held = HeldAsset;

            return held == null ? null : $"Holding: {held.Id} (slot {heldSlot.Value})";
        }

        public event Action Changed;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
        }

        public override void OnNetworkSpawn()
        {
            heldSlot.OnValueChanged += Moved;
            inventory.Changed += Relay;

            if (!IsServer) return;

            inventory.Changed += Release;
        }

        public override void OnNetworkDespawn()
        {
            heldSlot.OnValueChanged -= Moved;
            inventory.Changed -= Relay;

            if (!IsServer) return;

            inventory.Changed -= Release;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void HoldRpc(int slot)
        {
            Hold(slot);
        }

        public void Hold(int slot)
        {
            if (slot == Inventory.NoSlot)
            {
                heldSlot.Value = Inventory.NoSlot;
                return;
            }

            UniqueItem item = inventory.At(slot);
            if (item == null)
            {
                Log.Info($"Entity {name} can not hold slot {slot}: the slot is empty");
                return;
            }

            var asset = (UniqueItemAsset)Catalogs.Of<ItemCatalog>().Asset(item.AssetId);
            if (!asset.Equipable)
            {
                Log.Info($"Entity {name} can not put {item.AssetId} in hands");
                return;
            }

            heldSlot.Value = slot;
            Log.Info($"Entity {name} holds {item.AssetId} from slot {slot}");
        }

        private void Release()
        {
            if (heldSlot.Value == Inventory.NoSlot || Held != null) return;

            Log.Info($"Entity {name} holds nothing now: slot {heldSlot.Value} is empty");
            heldSlot.Value = Inventory.NoSlot;
        }

        private void Relay()
        {
            Changed?.Invoke();
        }

        private void Moved(int previous, int current)
        {
            Changed?.Invoke();
        }
    }
}
