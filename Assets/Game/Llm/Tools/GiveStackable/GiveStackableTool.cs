using Shooter.Game.Core;
using Shooter.Game.Loot;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(InventoryExchanger))]
    public sealed class GiveStackableTool : LlmTool<GiveStackableArguments>
    {
        private InventoryExchanger inventoryExchanger;

        public override string Name => "give_stackable";

        public override string Description =>
            @$"
Give some of your stackable items to a character within {inventoryExchanger.ExchangeRadius} meters.
The item is addressed by its exact name from your bag.
The recipient will automatically receive a notification.
";

        protected override void Awake()
        {
            base.Awake();
            inventoryExchanger = GetComponent<InventoryExchanger>();
        }

        protected override string Execute(GiveStackableArguments arguments, LlmCallContext context)
        {
            ItemSpec item = Catalogs.Of<ItemCatalog>().Of(arguments.Item);
            if (item == null) return $"There is no item named {arguments.Item}";

            if (item is not StackableItemSpec stackable)
                return
                    $"{arguments.Item} does not come in counted amounts, hand it over by its slot number with give_unique";

            return inventoryExchanger.GiveStackable(arguments.TargetId, stackable, arguments.Amount)
                ? $"Gave {arguments.Amount} x {arguments.Item} to {arguments.TargetId}"
                : "Could not give: the receiver is not around or you lack the items";
        }
    }
}
