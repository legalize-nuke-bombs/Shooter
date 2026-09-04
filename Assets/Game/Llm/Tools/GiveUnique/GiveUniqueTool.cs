using System;
using Shooter.Game.Loot;
using Shooter.Logging;

namespace Shooter.Game.Llm.GiveUnique
{
    [Serializable]
    public sealed class GiveUniqueTool : LlmTool<GiveUniqueArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private InventoryExchanger inventoryExchanger;

        public override string Name => "give_unique";

        public override string Description =>
            @$"
Give one of your unique items, by its slot number, to a character within {inventoryExchanger.ExchangeRadius} meters.
The recipient will automatically receive a notification.
";

        protected override void OnStart()
        {
            inventoryExchanger = Self.GetComponent<InventoryExchanger>();
            if (inventoryExchanger == null)
            {
                Log.Error($"Entity {Self.name} does not have inventory exchanger component required by tool {Name}");
            }
        }

        protected override string Execute(GiveUniqueArguments arguments, LlmCallContext context)
        {
            return inventoryExchanger.GiveUnique(arguments.TargetId, arguments.Slot)
                ? $"Gave the item from slot {arguments.Slot} to {arguments.TargetId}"
                : "Could not give: the receiver is not around or the slot is empty";
        }
    }
}
