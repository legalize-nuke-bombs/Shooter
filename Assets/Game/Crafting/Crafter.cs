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

        public bool TryCraft(StackableItemSpec[] input)
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
                    if (craft.Input[i] != input[i])
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

            return false;
        }

        private bool TryCraft(Craft craft)
        {
            Dictionary<StackableItemSpec, int> amountMap = craft.AmountMap();

            foreach (var kvp in amountMap)
            {
                if (inventory.StackableAmount(kvp.Key) < kvp.Value)
                {
                    return false;
                }
            }

            foreach (var kvp in amountMap)
            {
                inventory.RemoveStackable(kvp.Key, kvp.Value, InventoryOnConflict.Partly);
            }

            inventory.AddStackable(craft.Output, 1);
            return true;
        }
    }
}
