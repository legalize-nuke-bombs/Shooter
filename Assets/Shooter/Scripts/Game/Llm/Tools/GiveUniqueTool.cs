using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public sealed class GiveUniqueTool : LlmTool<GiveUniqueArguments>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private InventoryExchanger inventoryExchanger;

        public override string Name => "give_unique";

        public override string Description =>
            @$"
Give one of your unique items, by its slot number, to a character within {inventoryExchanger.Radius} meters.
The recipient will automatically receive a notification.
";

        protected override void HandleAttached()
        {
            inventoryExchanger = Self.GetComponent<InventoryExchanger>();
            if (inventoryExchanger == null)
            {
                Log.Error($"Entity {Self.name} does not have exchanger component required by tool {Name}");
            }
        }

        protected override string Execute(GiveUniqueArguments arguments, LlmCallContext context)
        {
            return inventoryExchanger.TryGiveUnique(Character.FindById(arguments.TargetId, RegisterInactivePolicy.Exclude), arguments.Slot)
                ? $"Gave the item from slot {arguments.Slot} to {arguments.TargetId}"
                : "Could not give: the receiver is not around or the slot is empty";
        }
    }
}
