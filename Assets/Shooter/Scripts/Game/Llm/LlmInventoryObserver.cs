using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(InventoryExchanger))]
    public class LlmInventoryObserver : MonoBehaviour
    {
        private InventoryExchanger inventoryExchanger;
        private Llm llm;

        private void Awake()
        {
            inventoryExchanger = GetComponent<InventoryExchanger>();
            llm = GetComponent<Llm>();
        }

        private void OnEnable()
        {
            inventoryExchanger.ReceivedFromCharacter += HandleReceivedFromCharacter;
        }

        private void OnDisable()
        {
            inventoryExchanger.ReceivedFromCharacter -= HandleReceivedFromCharacter;
        }

        private void HandleReceivedFromCharacter(Character giver, ItemAsset item, int amount)
        {
            if (item == null) return;

            long giverId = giver == null ? IncrementalId.None : giver.Id;

            llm.AppendEvent(item is StackableItemAsset
                ? $"[{Llm.FormatCurrentWorldTime()}] Character {giverId} gave you {item.Id} x {amount}"
                : $"[{Llm.FormatCurrentWorldTime()}] Character {giverId} gave you {item.Id}", true);
        }
    }
}
