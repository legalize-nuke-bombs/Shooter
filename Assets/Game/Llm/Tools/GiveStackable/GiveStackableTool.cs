using Shooter.Game.Loot;
using UnityEngine;

namespace Shooter.Game.Llm.Tools
{
    [RequireComponent(typeof(InventoryExchanger))]
    public sealed class GiveStackableTool : LlmTool<GiveStackableArguments>
    {
        private InventoryExchanger inventoryExchanger;

        protected override void Awake()
        {
            base.Awake();
            inventoryExchanger = GetComponent<InventoryExchanger>();
        }

        public override string Name => "give_stackable";

        public override string Description =>
            $"Give some of your stackable items to a character within {inventoryExchanger.ExchangeRadius} meters. The item is addressed by its exact name from your bag. Always tell the receiver what you gave.";

        protected override string Execute(GiveStackableArguments arguments)
        {
            ItemSpec item = Environment.Current.Items.FindByPromptName(arguments.Item);
            if (item == null) return $"There is no item named {arguments.Item}";

            return inventoryExchanger.GiveStackable(arguments.TargetId, item, arguments.Amount)
                ? $"Gave {arguments.Amount} x {arguments.Item} to {arguments.TargetId}"
                : "Could not give: the receiver is not around or you lack the items";
        }
    }
}
