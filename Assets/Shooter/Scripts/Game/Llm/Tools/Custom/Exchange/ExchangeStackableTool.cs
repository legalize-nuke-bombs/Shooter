using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public sealed class ExchangeStackableTool : LlmTool<ExchangeStackableArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private InventoryExchanger inventoryExchanger;
        private Inventory inventory;

        public override string Name => "exchange_stackable";

        public override string Description =>
            @$"
Hand over and take stackable items, addressed by their exact names and amounts, with a character or a lootable entity (chest, corpse, etc.) within {inventoryExchanger.Radius} meters.
`give` lists what you hand over from your bag, `take` lists what you take into your bag; either may be empty, and both may be done in one call.
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

        protected override string Execute(ExchangeStackableArguments arguments, LlmCallContext context)
        {
            StackableAmount[] give = arguments.Give ?? Array.Empty<StackableAmount>();
            StackableAmount[] take = arguments.Take ?? Array.Empty<StackableAmount>();
            if (give.Length == 0 && take.Length == 0) return "Nothing to give or take";

            if (!ExchangeTarget.TryFind(arguments.TargetId, take.Length > 0, inventoryExchanger, out Inventory target, out string refusal)) return refusal;

            StringBuilder result = new();
            foreach (StackableAmount request in give)
            {
                result.AppendLine(Give(request, target));
            }

            foreach (StackableAmount request in take)
            {
                result.AppendLine(Take(request, target));
            }

            return result.ToString();
        }

        private string Give(StackableAmount request, Inventory target)
        {
            if (!TryFindStackable(request, out StackableItemAsset asset, out string refusal)) return refusal;

            int held = inventory.CountStackable(asset);
            return inventoryExchanger.TryGiveStackable(target, asset, request.Amount)
                ? $"Gave {request.Amount} x {request.Item}"
                : $"Could not give {request.Amount} x {request.Item}: you have {held}";
        }

        private string Take(StackableAmount request, Inventory target)
        {
            if (!TryFindStackable(request, out StackableItemAsset asset, out string refusal)) return refusal;

            int there = target.CountStackable(asset);
            return inventoryExchanger.TryTakeStackable(target, asset, request.Amount)
                ? $"Took {request.Amount} x {request.Item}"
                : $"Could not take {request.Amount} x {request.Item}: there are {there}";
        }

        private static bool TryFindStackable(StackableAmount request, out StackableItemAsset asset, out string refusal)
        {
            asset = null;
            refusal = null;

            if (request.Amount <= 0)
            {
                refusal = $"{request.Item}: the amount must be above zero";
                return false;
            }

            ItemAsset item = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(request.Item);
            if (item == null)
            {
                refusal = $"There is no item named {request.Item}";
                return false;
            }

            asset = item as StackableItemAsset;
            if (asset == null)
            {
                refusal = $"{request.Item} does not come in counted amounts, exchange it by its slot number with exchange_unique";
                return false;
            }

            return true;
        }
    }
}
