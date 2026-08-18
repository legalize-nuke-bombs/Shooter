using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.AI.Healer
{
    public class AIHealer : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float timerInterval = 2.5f;
        [SerializeField] private float healthThreshold = 95f;
        [SerializeField] private float underHealPenaltyMultiplier = 1f;
        [SerializeField] private float overHealBasePenaltyMultiplier = 1f;

        private Inventory inventory;
        private Health health;
        private Dictionary<FixedString32Bytes, int> healingItems;

        public struct OnAutoHealCallbackData
        {
            public StackableItemSpec Item;
            public int StartHp;
            public int EndHp;
        }
        public event Action<OnAutoHealCallbackData> OnAutoHealCallback;

        private float timer;

        private void Awake()
        {
            inventory = this.Find<Inventory>();
            health = this.Find<Health>();
            healingItems = Catalogs.Of<ItemCatalog>().FindAll(item =>
                    item is StackableItemSpec stackableItem && stackableItem.HealMarker > 0)
                .Cast<StackableItemSpec>()
                .ToDictionary(stackableItem => stackableItem.Id, stackableItem => stackableItem.HealMarker);
        }

        private void Update()
        {
            if (!IsServer) return;
            timer += Time.deltaTime;
            if (timer >= timerInterval)
            {
                Tick();
                timer -= timerInterval;
            }
        }

        private void Tick()
        {
            double healthAmount = health.Hp;
            if (healthAmount >= healthThreshold) return;

            double missing = health.MaxHp - healthAmount;
            double safetyCoefficient = healthAmount / health.MaxHp;

            ItemCatalog itemCatalog = Catalogs.Of<ItemCatalog>();

            FixedString32Bytes bestItemId = null;
            double lowestScore = missing * underHealPenaltyMultiplier;

            foreach (var kvp in healingItems)
            {
                var itemSpec = itemCatalog.Of(kvp.Key) as StackableItemSpec;
                if (inventory.StackableAmount(itemSpec) <= 0)
                {
                    continue;
                }

                double healAmount = kvp.Value;
                double score = healAmount <= missing
                    ? (missing - healAmount) * underHealPenaltyMultiplier
                    : (healAmount - missing) * overHealBasePenaltyMultiplier * safetyCoefficient;

                if (score < lowestScore)
                {
                    lowestScore = score;
                    bestItemId = kvp.Key;
                }
            }

            if (bestItemId != null)
            {
                Log.Info($"Entity {this.NameOf()} decided to use {bestItemId} ({healingItems[bestItemId]}), missing {missing}, safety coefficient {safetyCoefficient}, under heal coefficient {underHealPenaltyMultiplier}, over heal base coefficient {overHealBasePenaltyMultiplier}");
                inventory.UseStackable(bestItemId);
                if (OnAutoHealCallback != null)
                {
                    OnAutoHealCallback.Invoke(new OnAutoHealCallbackData()
                    {
                        Item = itemCatalog.Of(bestItemId) as StackableItemSpec,
                        StartHp = (int)healthAmount,
                        EndHp = (int)health.Hp
                    });
                }
            }
        }
    }
}
