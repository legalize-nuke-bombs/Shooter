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
            slots.Clear();
            foreach (int slot in slots)
            {
                sd.Slots.Add(slot);
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
            inventory.Changed += HandleInventoryChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            inventory.Changed -= HandleInventoryChanged;
        }

        private void HandleInventoryChanged()
        {
            // Backward loop allows safe removing on iteration
            bool modification = false;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                int slot = slots[i];
                UniqueItem item = inventory.GetUniqueAt(slot);
                if (item == null || item is not Artifact)
                {
                    slots.RemoveAt(i);
                    modification = true;
                }
            }

            if (modification)
            {
                Changed?.Invoke();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void HoldSlotRpc(int slot)
        {
            TryHoldSlot(slot, out string result);
            Log.Info($"Entity {name} inventory artefact holder hold slot rpc result: {result}");
        }

        public void TryHoldSlot(int slot, out string result)
        {
            if (slots.Count >= maxSlotsNum)
            {
                result = $"Failed to hold slot: too many slots held (current {slots.Count} max {maxSlotsNum})";
                return;
            }
            if (slots.Contains(slot))
            {
                result = $"Failed to hold slot: {slot} is already held";
                return;
            }
            UniqueItem item = inventory.GetUniqueAt(slot);
            if (item == null)
            {
                result = $"Failed to hold slot: {slot} is empty";
                return;
            }
            if (item is not Artifact)
            {
                result = $"Failed to hold slot: {slot} is not an artifact";
                return;
            }
            slots.Add(slot);
            result = $"Successfully held {item.AssetId} (slot {slot})";
        }
    }
}
