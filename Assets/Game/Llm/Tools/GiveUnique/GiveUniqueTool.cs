using Shooter.Game.Loot;
using UnityEngine;

namespace Shooter.Game.Llm.Tools
{
    [RequireComponent(typeof(InventoryExchanger))]
    public sealed class GiveUniqueTool : LlmTool<GiveUniqueArguments>
    {
        private InventoryExchanger inventoryExchanger;

        protected override void Awake()
        {
            base.Awake();
            inventoryExchanger = GetComponent<InventoryExchanger>();
        }

        public override string Name => "give_unique";

        public override string Description =>
            $"Give one of your unique items, by its slot number, to a character within {inventoryExchanger.ExchangeRadius} meters.";

        protected override string Execute(GiveUniqueArguments arguments)
        {
            return inventoryExchanger.GiveUnique(arguments.TargetId, arguments.Slot)
                ? $"Gave the item from slot {arguments.Slot} to {arguments.TargetId}"
                : "Could not give: the receiver is not around or the slot is empty";
        }
    }
}
