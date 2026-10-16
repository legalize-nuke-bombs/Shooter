using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(InventoryExchanger))]
    public class LlmInventoryObserver : MonoBehaviour
    {
        private InventoryExchanger exchanger;
        private Llm llm;

        private void Awake()
        {
            exchanger = GetComponent<InventoryExchanger>();
            llm = GetComponent<Llm>();
        }

        private void OnEnable()
        {
            exchanger.Received += Received;
        }

        private void OnDisable()
        {
            exchanger.Received -= Received;
        }

        private void Received(Character from, ItemSpec item, int amount)
        {
            if (item == null) return;

            long giver = from == null ? GameObjectRuntimeId.Default : from.Id;

            llm.Notice(item is StackableItemSpec
                ? $"[{Llm.Stamp()}] Character {giver} gave you {item.Key} x {amount}"
                : $"[{Llm.Stamp()}] Character {giver} gave you {item.Key}", true);
        }
    }
}
