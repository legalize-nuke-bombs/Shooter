using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.AI.Eater
{
    public class AIEater : NetworkBehaviour
    {
        [SerializeField] private float timerInterval = 2.5f;

        [SerializeField] private float hungerThreshold = 20f;
        private List<FixedString32Bytes> foodIds;
        private Hunger hunger;
        private Inventory inventory;

        private float timer;

        private void Awake()
        {
            inventory = this.Find<Inventory>();
            hunger = this.Find<Hunger>();
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
                if (inventory.UseStackable(foodId))
                    break;
        }
    }
}
