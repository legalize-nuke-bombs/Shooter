using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shooter.Game.AI.Eater
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Hunger))]
    public class AIEater : NetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float timerInterval = 2.5f;
        [SerializeField] private float hungerThreshold = 20f;

        private Hunger hunger;
        private Inventory inventory;
        private List<FixedString32Bytes> foodIds;

        public struct OnAutoEatCallbackData
        {
            public StackableItemSpec Item;
            public int StartSaturation;
            public int EndSaturation;
        }
        public Action<OnAutoEatCallbackData> OnAutoEatCallback;

        private float timer;

        public string ComponentKey => "AIEater";
        private struct SaveData
        {
            public bool Enabled { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Enabled = enabled
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            enabled = sd.Enabled;
        }

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            hunger = GetComponent<Hunger>();
            foodIds = Catalogs.Of<ItemCatalog>().FindAll(item =>
                    item is StackableItemSpec stackableItem && stackableItem.FoodMarker > 0)
                .Cast<StackableItemSpec>()
                .Select(stackableItem => stackableItem.Id)
                .ToList();
            timer = Random.Range(0f, timerInterval);
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
            float hungerAmount = hunger.Amount;
            if (hungerAmount >= hungerThreshold) return;

            foreach (FixedString32Bytes foodId in foodIds)
            {
                if (inventory.UseStackable(foodId))
                {
                    Log.Info($"Entity {name} decided to eat {foodId} on {hungerAmount} saturation");
                    if (OnAutoEatCallback != null)
                    {
                        OnAutoEatCallback.Invoke(new OnAutoEatCallbackData()
                        {
                            Item = Catalogs.Of<ItemCatalog>().Of(foodId) as StackableItemSpec,
                            StartSaturation = (int)hungerAmount,
                            EndSaturation = (int)hunger.Amount
                        });
                    }
                    break;
                }
            }

        }
    }
}
