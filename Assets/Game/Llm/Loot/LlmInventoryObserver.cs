using Shooter.Game.Core;
using Shooter.Game.Loot;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(Exchanger))]
    public class LlmInventoryObserver : MonoBehaviour
    {
        private Exchanger exchanger;
        private Llm llm;

        private void Awake()
        {
            exchanger = GetComponent<Exchanger>();
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
