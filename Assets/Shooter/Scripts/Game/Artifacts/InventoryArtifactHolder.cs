using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    /*
     * TODO
     * Не проверял компиляцию и работу скорее всего есть проблемы
     */
    [RequireComponent(typeof(Inventory))]
    public class InventoryArtifactHolder : ShooterNetworkBehaviour, ISaveableComponent, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private int maxSlotsNum = 6;

        private readonly NetworkList<int> slots = new();

        private Inventory inventory;

        public event Action Changed;

        private struct SaveData
        {
            public List<int> Slots { get; set; }
        }
        public object SaveObject()
        {
            SaveData sd = new SaveData()
            {
                Slots = new List<int>()
            };
            foreach (int slot in slots)
            {
                sd.Slots.Add(slot);
            }
            return sd;
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.ToObject<SaveData>();
            foreach (int slot in sd.Slots)
            {
                InventoryArtifactHolderTryHoldResultCode result = TryHoldSlot(slot);
                if (result != InventoryArtifactHolderTryHoldResultCode.Ok) Log.Warn($"Entity {name} can not hold saved artifact slot {slot}: {result}");
            }
        }

        public IEnumerator<int> HeldSlots => slots.GetEnumerator();

        public DigestionPriority Priority => DigestionPriority.Low;
        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief)
            {
                return null;
            }

            if (slots.Count == 0)
            {
                return "No artifacts held";
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("Held artifacts: ");
            foreach (int slot in slots)
            {
                UniqueItem item = inventory.GetUniqueAt(slot);
                sb.Append($"{item.AssetId} (slot {slot}) ");
            }
            return sb.ToString();
        }

        protected override void ShooterAwake()
        {
            inventory = GetComponent<Inventory>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            enabled = IsServer;

            if (IsServer)
            {
                inventory.UniqueRemoved += HandleUniqueRemoved;
            }
            else
            {
                slots.OnListChanged += HandleSlotsReplicated;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            enabled = false;

            if (IsServer)
            {
                inventory.UniqueRemoved -= HandleUniqueRemoved;
            }
            else
            {
                slots.OnListChanged -= HandleSlotsReplicated;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void HoldSlotRpc(int slot)
        {
            InventoryArtifactHolderTryHoldResultCode result = TryHoldSlot(slot);
            Log.Info($"Entity {name} inventory artefact holder hold slot rpc result code: {result}");
        }

        public enum InventoryArtifactHolderTryHoldResultCode
        {
            TooManySlots,
            SlotAlreadyHeld,
            SlotIsEmpty,
            SlotNotAnArtifact,
            Ok
        }
        public InventoryArtifactHolderTryHoldResultCode TryHoldSlot(int slot)
        {
            if (slots.Count >= maxSlotsNum)
            {
                return InventoryArtifactHolderTryHoldResultCode.TooManySlots;
            }
            if (slots.Contains(slot))
            {
                return InventoryArtifactHolderTryHoldResultCode.SlotAlreadyHeld;
            }
            UniqueItem item = inventory.GetUniqueAt(slot);
            if (item == null)
            {
                return InventoryArtifactHolderTryHoldResultCode.SlotIsEmpty;
            }
            if (item is not Artifact)
            {
                return InventoryArtifactHolderTryHoldResultCode.SlotNotAnArtifact;
            }
            slots.Add(slot);
            (item as Artifact).Enable(gameObject);
            Changed?.Invoke();
            return InventoryArtifactHolderTryHoldResultCode.Ok;
        }

        private void HandleUniqueRemoved(int slot, UniqueItem item)
        {
            if (!slots.Remove(slot))
            {
                Log.Info($"Entity {name} holds no artifact in slot {slot}: nothing to stop, {item.AssetId} left it");
                return;
            }

            (item as Artifact).Disable();
            Log.Info($"Entity {name} stops holding artifact {item.AssetId}: it left slot {slot}");
            Changed?.Invoke();
        }

        private void HandleSlotsReplicated(NetworkListEvent<int> change)
        {
            Changed?.Invoke();
        }

        private void Update()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                int slot = slots[i];
                Artifact artifact = inventory.GetUniqueAt(slot) as Artifact;
                artifact.Update(Time.deltaTime);
            }
        }
    }
}
