using System;
using Shooter.Infrastructure;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game
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
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            InventoryUser user = Agent.Value.GetComponent<InventoryUser>();
            Hunger hunger = Agent.Value.GetComponent<Hunger>();
            if (user == null || hunger == null) return Status.Failure;

            foreach (ItemAsset item in Catalogs.GetCatalog<ItemCatalog>().FindAll(item =>
                         item is StackableItemAsset stackableItem && stackableItem.FoodMarker > 0))
            {
                int startSaturation = (int)hunger.Amount;
                if (user.Use((StackableItemAsset)item))
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
