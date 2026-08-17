using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Body;
using Shooter.Game.Loot;
using Shooter.Game.World;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Game.AI.Eater
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Hunger))]
    public class AIEater : NetworkBehaviour
    {
        private Inventory inventory;
        private Hunger hunger;
        private List<FixedString32Bytes> foodIds;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            hunger = GetComponent<Hunger>();
            foodIds = Catalogs.Of<ItemCatalog>().FindAll(
                item => (item is StackableItemSpec stackableItem && stackableItem.FoodMarker > 0))
                .Cast<StackableItemSpec>()
                .Select(stackableItem => stackableItem.Id)
                .ToList();
            timer = Random.Range(0f, timerInterval);
        }

        private float timer;
        [SerializeField] private float timerInterval = 2.5f;
        private void Update()
        {
            if (!IsServer)
            {
                return;
            }
            timer += Time.deltaTime;
            if (timer >= timerInterval)
            {
                Tick();
                timer -= timerInterval;
            }
        }

        [SerializeField] private float hungerThreshold = 20f;
        private void Tick()
        {
            float hungerAmount = hunger.Amount;
            if (hungerAmount >= hungerThreshold)
            {
                return;
            }

            foreach (FixedString32Bytes foodId in foodIds)
            {
                if (inventory.UseStackable(foodId))
                {
                    break;
                }
            }
        }
    }
}
