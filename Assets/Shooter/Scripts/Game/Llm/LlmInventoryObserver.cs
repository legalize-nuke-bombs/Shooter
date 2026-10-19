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
            exchanger.ReceivedFromCharacter += Received;
        }

        private void OnDisable()
        {
            exchanger.ReceivedFromCharacter -= Received;
        }

        private void Received(Character from, ItemAsset item, int amount)
        {
            if (item == null) return;

            long giver = from == null ? CharacterId.None : from.Id;

            llm.Notice(item is StackableItemAsset
                ? $"[{Llm.Stamp()}] Character {giver} gave you {item.Id} x {amount}"
                : $"[{Llm.Stamp()}] Character {giver} gave you {item.Id}", true);
        }
    }
}
