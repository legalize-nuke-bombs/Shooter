using Shooter.Game.Core;
using Shooter.Game.Loot;
using UnityEngine;

namespace Shooter.Game.Llm
{
    // The mind learns who handed it what; it does not wake for that
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(Inventory))]
    public class LlmInventoryObserver : MonoBehaviour
    {
        private Inventory inventory;
        private Llm llm;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            llm = GetComponent<Llm>();
        }

        private void OnEnable()
        {
            inventory.Received += Received;
        }

        private void OnDisable()
        {
            inventory.Received -= Received;
        }

        private void Received(Character from, ItemSpec item, int amount)
        {
            if (item == null) return;

            long giver = from == null ? GameObjectRuntimeId.Default : from.Id;

            llm.Notice(item is StackableItemSpec
                ? $"[{Llm.Stamp()}] Character {giver} gave you {item.Key} x {amount}"
                : $"[{Llm.Stamp()}] Character {giver} gave you {item.Key}", false);
        }
    }
}
