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
        name: "Use Healing Item",
        description: "Uses the healing item that fits the missing health best.",
        story: "[Agent] uses healing item",
        category: "Action",
        id: "9c2e6f0a4b1d4e28a5b7c3d9e1f20a04")]
    public partial class UseHealingItemAction : Action
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<float> UnderHealPenalty = new(1f);
        [SerializeReference] public BlackboardVariable<float> OverHealBasePenalty = new(1f);

        protected override Status OnStart()
        {
            if (Agent.Value == null) return Status.Failure;
            Inventory inventory = Agent.Value.GetComponent<Inventory>();
            InventoryUser inventoryUser = Agent.Value.GetComponent<InventoryUser>();
            Health health = Agent.Value.GetComponent<Health>();
            if (inventory == null || inventoryUser == null || health == null) return Status.Failure;

            double missingHp = health.MaxHp - health.Hp;
            double safetyCoefficient = health.Hp / health.MaxHp;

            StackableItemAsset bestItem = null;
            double lowestScore = missingHp * UnderHealPenalty.Value;
            int candidateCount = 0;

            foreach (ItemAsset item in Catalogs.GetCatalog<ItemCatalog>().FindAll(candidate => candidate is StackableItemAsset stackableItem && stackableItem.Stats.Heal > 0))
            {
                StackableItemAsset stackableItem = (StackableItemAsset)item;
                if (inventory.CountStackable(stackableItem) <= 0) continue;

                candidateCount++;
                double healAmount = stackableItem.Stats.Heal;
                double score = healAmount <= missingHp
                    ? (missingHp - healAmount) * UnderHealPenalty.Value
                    : (healAmount - missingHp) * OverHealBasePenalty.Value * safetyCoefficient;

                if (score < lowestScore)
                {
                    lowestScore = score;
                    bestItem = stackableItem;
                }
            }

            if (bestItem == null)
            {
                Log.Info($"Entity {Agent.Value.name} refused healing: {candidateCount} candidates in the bag, missing {missingHp}, safety {safetyCoefficient}");
                return Status.Failure;
            }

            int hpBefore = (int)health.Hp;
            if (!inventoryUser.TryUseItem(bestItem))
            {
                Log.Error($"Entity {Agent.Value.name} could not use heal {bestItem.Id} it has in the bag");
                return Status.Failure;
            }

            Log.Info($"Entity {Agent.Value.name} used heal {bestItem.Id} by behavior graph, missing {missingHp}");
            BehaviorReporter behaviorReporter = Agent.Value.GetComponent<BehaviorReporter>();
            if (behaviorReporter != null)
            {
                behaviorReporter.Report(new BehaviorReport
                {
                    Prompt = $"Your character automatically healed using {bestItem.Id} ({hpBefore}hp -> {(int)health.Hp}hp)"
                });
            }
            return Status.Success;
        }
    }
}
