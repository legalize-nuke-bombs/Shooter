using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public sealed class GiveStackableTool : LlmTool<GiveStackableArguments>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private InventoryExchanger inventoryExchanger;

        public override string Name => "give_stackable";

        public override string Description =>
            @$"
Give some of your stackable items to a character within {inventoryExchanger.Radius} meters.
The item is addressed by its exact name from your bag.
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

        protected override string Execute(GiveStackableArguments arguments, LlmCallContext context)
        {
            ItemAsset item = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(arguments.Item);
            if (item == null) return $"There is no item named {arguments.Item}";

            if (item is not StackableItemAsset stackableItem) return $"{arguments.Item} does not come in counted amounts, hand it over by its slot number with give_unique";

            return inventoryExchanger.TryGiveStackable(Character.FindById(arguments.TargetId, RegisterInactivePolicy.Exclude), stackableItem, arguments.Amount)
                ? $"Gave {arguments.Amount} x {arguments.Item} to {arguments.TargetId}"
                : "Could not give: the receiver is not around or you lack the items";
        }
    }
}
