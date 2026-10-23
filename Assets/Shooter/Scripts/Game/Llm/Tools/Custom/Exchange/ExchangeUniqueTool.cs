using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public sealed class ExchangeUniqueTool : LlmTool<ExchangeUniqueArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private InventoryExchanger inventoryExchanger;
        private Inventory inventory;

        public override string Name => "exchange_unique";

        public override string Description =>
            @$"
Hand over and take unique items, addressed by their slot numbers, with a character or a lootable entity (chest, corpse, etc.) within {inventoryExchanger.Radius} meters.
`give` lists the slots of your bag to hand over, `take` lists the slots of the lootable entity to take into your bag, as its detailed look shows them; either may be empty, and both may be done in one call.
Things are taken only from lootable entities: a character has to give them to you.
";

        protected override void HandleAttached()
        {
            inventoryExchanger = Self.GetComponent<InventoryExchanger>();
            inventory = Self.GetComponent<Inventory>();
            if (inventoryExchanger == null)
            {
                Log.Error($"Entity {Self.name} does not have exchanger component required by tool {Name}");
            }
        }

        protected override string Execute(ExchangeUniqueArguments arguments, LlmCallContext context)
        {
            int[] give = arguments.Give ?? Array.Empty<int>();
            int[] take = arguments.Take ?? Array.Empty<int>();
            if (give.Length == 0 && take.Length == 0) return "Nothing to give or take";

            if (!ExchangeTarget.TryFind(arguments.TargetId, take.Length > 0, inventoryExchanger, out Inventory target, out string refusal)) return refusal;

            StringBuilder result = new();
            foreach (int slot in give)
            {
                UniqueItem item = inventory.GetUniqueAt(slot);
                result.AppendLine(item == null
                    ? $"Your slot {slot} is empty"
                    : inventoryExchanger.TryGiveUnique(target, slot) ? $"Gave {item.AssetId} from your slot {slot}" : $"Could not give {item.AssetId} from your slot {slot}");
            }

            foreach (int slot in take)
            {
                UniqueItem item = target.GetUniqueAt(slot);
                result.AppendLine(item == null
                    ? $"Slot {slot} there is empty"
                    : inventoryExchanger.TryTakeUnique(target, slot) ? $"Took {item.AssetId} from slot {slot}" : $"Could not take {item.AssetId} from slot {slot}");
            }

            return result.ToString();
        }
    }
}
