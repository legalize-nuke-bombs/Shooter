using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Llm))]
    [RequireComponent(typeof(InventoryExchanger))]
    public class LlmInventoryObserver : ShooterComponent
    {
        private InventoryExchanger inventoryExchanger;
        private Llm llm;

        protected override void ShooterAwake()
        {
            inventoryExchanger = GetComponent<InventoryExchanger>();
            llm = GetComponent<Llm>();
        }

        protected override void ShooterOnEnable()
        {
            inventoryExchanger.ReceivedFromCharacter += HandleReceivedFromCharacter;
        }

        protected override void ShooterOnDisable()
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
