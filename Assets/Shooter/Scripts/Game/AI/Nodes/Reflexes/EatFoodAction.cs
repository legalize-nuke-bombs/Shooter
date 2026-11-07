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
        story: "[Agent] eats food from inventory",
        category: "Action",
        id: "323decc7409441e1a70dc74fea2b63a1")]
    public partial class EatFoodAction : Action
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<string> EatenItem = new();
        [SerializeReference] public BlackboardVariable<float> SaturationBefore = new();
        [SerializeReference] public BlackboardVariable<float> SaturationAfter = new();

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            InventoryUser inventoryUser = Agent.Value.GetComponent<InventoryUser>();
            Hunger hunger = Agent.Value.GetComponent<Hunger>();
            if (inventoryUser == null || hunger == null) return Status.Failure;

            foreach (ItemAsset item in Catalogs.GetCatalog<ItemCatalog>().FindAll(candidate => candidate is StackableItemAsset stackableItem && stackableItem.Stats.Food > 0))
            {
                float saturationBefore = hunger.Amount;
                if (inventoryUser.TryUseItem((StackableItemAsset)item))
                {
                    Log.Info($"Entity {Agent.Value.name} ate {item.Id} by behavior graph");
                    EatenItem.Value = item.Id;
                    SaturationBefore.Value = saturationBefore;
                    SaturationAfter.Value = hunger.Amount;
                    return Status.Success;
                }
            }

            return Status.Failure;
        }
    }
}
