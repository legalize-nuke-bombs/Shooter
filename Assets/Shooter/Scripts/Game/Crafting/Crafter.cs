using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    public class Crafter : ShooterNetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private List<CraftAsset> availableCrafts;
        private readonly Dictionary<string, CraftAsset> craftsById = new();

        private Inventory inventory;

        public event Action<CraftAsset> Crafted;

        protected override void ShooterAwake()
        {
            inventory = GetComponent<Inventory>();
            foreach (CraftAsset craft in availableCrafts)
            {
                craftsById.Add(craft.Id, craft);
            }
        }

        public List<CraftAsset> AvailableCrafts => availableCrafts;

        public CraftAsset FindKnownCraft(string craftId)
        {
            if (string.IsNullOrEmpty(craftId)) return null;
            return craftsById.GetValueOrDefault(craftId, null);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void CraftRpc(FixedString32Bytes craftId)
        {
            if (TryCraft(FindKnownCraft(craftId.ToString()))) return;

            Log.Info($"Entity {name} failed to craft {craftId} rpc");
        }

        public bool TryCraft(CraftAsset craft)
        {
            if (craft == null || !availableCrafts.Contains(craft))
            {
                Log.Info($"Entity {name} does not know the craft {(craft == null ? "null" : craft.Id)}");
                return false;
            }

            Dictionary<StackableItemAsset, int> inputAmounts = craft.CountInputAmounts();

            foreach (KeyValuePair<StackableItemAsset, int> inputAmount in inputAmounts)
            {
                int availableAmount = inventory.CountStackable(inputAmount.Key);
                if (availableAmount < inputAmount.Value)
                {
                    Log.Info($"Entity {name} lacks {inputAmount.Key.Id} for {craft.Id}: {availableAmount} of {inputAmount.Value}");
                    return false;
                }
            }

            foreach (KeyValuePair<StackableItemAsset, int> inputAmount in inputAmounts)
            {
                inventory.RemoveStackable(inputAmount.Key, inputAmount.Value, InventoryShortagePolicy.Partly);
            }

            if (craft.Output is StackableItemAsset stackableOutput)
            {
                inventory.AddStackable(stackableOutput, 1);
            }
            else if (craft.Output is UniqueItemAsset uniqueOutput)
            {
                inventory.AddUnique(uniqueOutput.CreateItem());
            }
            else
            {
                Log.Error("Unexpected craft item output");
            }

            Log.Info($"Entity {name} crafted {craft.Id}");
            Crafted?.Invoke(craft);
            return true;
        }
    }
}
