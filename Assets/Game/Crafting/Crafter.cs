using System.Collections.Generic;
using Shooter.Game.Body;
using Shooter.Game.Loot;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Crafting
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Speaker))]
    public class Crafter : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private List<Craft> availableCrafts;
        private readonly Dictionary<string, Craft> craftsById = new Dictionary<string, Craft>();

        private Inventory inventory;
        private Speaker speaker;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            speaker = GetComponent<Speaker>();
            foreach (Craft craft in availableCrafts)
            {
                craftsById.Add(craft.Key, craft);
            }
        }

        public List<Craft> AvailableCrafts => availableCrafts;

        public Craft Known(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return craftsById.GetValueOrDefault(id, null);
        }

        public bool TryCraft(Craft craft)
        {
            if (craft == null || !availableCrafts.Contains(craft))
            {
                Log.Info($"Entity {name} does not know the craft {(craft == null ? "null" : craft.Key)}");
                return false;
            }

            Dictionary<StackableItemSpec, int> amountMap = craft.AmountMap();

            foreach (var kvp in amountMap)
            {
                int available = inventory.StackableAmount(kvp.Key);
                if (available < kvp.Value)
                {
                    Log.Info($"Entity {name} lacks {kvp.Key.Key} for {craft.Key}: {available} of {kvp.Value}");
                    return false;
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

            Log.Info($"Entity {name} crafted {craft.Key}");
            if (craft.Sound != null) speaker.Play(craft.Sound);
            return true;
        }
    }
}
