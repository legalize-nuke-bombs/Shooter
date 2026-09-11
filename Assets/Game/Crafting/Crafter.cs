using System;
using System.Collections.Generic;
using Shooter.Game.Loot;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Crafting
{
    [RequireComponent(typeof(Inventory))]
    public class Crafter : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private List<Craft> availableCrafts;

        private Inventory inventory;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
        }

        public List<Craft> AvailableCrafts => availableCrafts;

        public ItemSpec TryCraft(string[] input)
        {
            if (input.Length != 9)
            {
                throw new ArgumentException("input length must be 9");
            }

            foreach (Craft craft in availableCrafts)
            {
                bool match = true;
                for (int i = 0; i < 9; i++)
                {
                    if ((input[i] != null && input[i] != "" && input[i] != "null" && craft.Input[i] == null) || craft.Input[i].Id != input[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return TryCraft(craft);
                }
            }

            return null;
        }

        private ItemSpec TryCraft(Craft craft)
        {
            Dictionary<StackableItemSpec, int> amountMap = craft.AmountMap();

            foreach (var kvp in amountMap)
            {
                if (inventory.StackableAmount(kvp.Key) < kvp.Value)
                {
                    return null;
                }
            }

            foreach (var kvp in amountMap)
            {
                inventory.RemoveStackable(kvp.Key, kvp.Value, InventoryOnConflict.Partly);
            }

            if (craft.Output is StackableItemSpec stackableOutput)
            {
                inventory.AddStackable(stackableOutput, 1);
            }
            else if (craft.Output is UniqueItemSpec uniqueOutput)
            {
                inventory.Put(uniqueOutput.Create());
            }
            else
            {
                Log.Error("Unexpected craft item output");
            }

            Log.Info($"Entity {name} crafted {craft.Id}");
            return craft.Output;
        }
    }
}
