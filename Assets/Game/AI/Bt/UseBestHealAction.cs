using System;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Behavior;
using Unity.Collections;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Shooter.Game.AI.Bt
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Use Best Heal",
        description: "Uses the healing item that fits the missing health best.",
        story: "[Agent] uses best healing item",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a04")]
    public partial class UseBestHealAction : Action
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<float> UnderHealPenalty = new(1f);
        [SerializeReference] public BlackboardVariable<float> OverHealBasePenalty = new(1f);

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            Inventory inventory = Agent.Value.GetComponent<Inventory>();
            Health health = Agent.Value.GetComponent<Health>();
            if (inventory == null || health == null) return Status.Failure;

            double missing = health.MaxHp - health.Hp;
            double safetyCoefficient = health.Hp / health.MaxHp;

            FixedString32Bytes? bestItemId = null;
            double lowestScore = missing * UnderHealPenalty.Value;

            foreach (ItemSpec item in Catalogs.Of<ItemCatalog>().FindAll(item =>
                         item is StackableItemSpec stackableItem && stackableItem.HealMarker > 0))
            {
                StackableItemSpec stackableItem = (StackableItemSpec)item;
                if (inventory.StackableAmount(stackableItem) <= 0) continue;

                double healAmount = stackableItem.HealMarker;
                double score = healAmount <= missing
                    ? (missing - healAmount) * UnderHealPenalty.Value
                    : (healAmount - missing) * OverHealBasePenalty.Value * safetyCoefficient;

                if (score < lowestScore)
                {
                    lowestScore = score;
                    bestItemId = stackableItem.Id;
                }
            }

            if (bestItemId == null) return Status.Failure;

            Log.Info($"Entity {Agent.Value.name} used heal {bestItemId.Value} by behavior graph, missing {missing}");
            inventory.UseStackable(bestItemId.Value);
            return Status.Success;
        }
    }
}
