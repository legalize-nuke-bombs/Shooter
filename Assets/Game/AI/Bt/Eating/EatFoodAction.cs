using System;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt.Eating
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Eat Food",
        description: "Eats the first available food item from the inventory.",
        story: "[Agent] eats food from inventory",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a03")]
    public partial class EatFoodAction : Action
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            Inventory inventory = Agent.Value.GetComponent<Inventory>();
            Hunger hunger = Agent.Value.GetComponent<Hunger>();
            if (inventory == null || hunger == null) return Status.Failure;

            foreach (ItemSpec item in Catalogs.Of<ItemCatalog>().FindAll(item =>
                         item is StackableItemSpec stackableItem && stackableItem.FoodMarker > 0))
            {
                int startSaturation = (int)hunger.Amount;
                if (inventory.UseStackable(item.Id))
                {
                    Log.Info($"Entity {Agent.Value.name} ate {item.Id} by behavior graph");
                    BtReports reports = Agent.Value.GetComponent<BtReports>();
                    if (reports != null)
                    {
                        reports.Report(new BtReport
                        {
                            Prompt = $"Your character automatically ate {item.Id} ({startSaturation} saturation -> {(int)hunger.Amount} saturation)"
                        });
                    }
                    return Status.Success;
                }
            }

            return Status.Failure;
        }
    }
}
