using Shooter.Game.Core;
using Shooter.Game.Loot;

namespace Shooter.Game.Llm
{
    public sealed class GiveUniqueTool : LlmTool<GiveUniqueArguments>
    {
        private InventoryExchanger inventoryExchanger;

        public override string Name => "give_unique";

        public override string Description =>
            @$"
Give one of your unique items, by its slot number, to a character within {inventoryExchanger.ExchangeRadius} meters.
The recipient will automatically receive a notification.
";

        protected override void Awake()
        {
            base.Awake();
            inventoryExchanger = this.Find<InventoryExchanger>();
        }

        protected override string Execute(GiveUniqueArguments arguments)
        {
            return inventoryExchanger.GiveUnique(arguments.TargetId, arguments.Slot)
                ? $"Gave the item from slot {arguments.Slot} to {arguments.TargetId}"
                : "Could not give: the receiver is not around or the slot is empty";
        }
    }
}
