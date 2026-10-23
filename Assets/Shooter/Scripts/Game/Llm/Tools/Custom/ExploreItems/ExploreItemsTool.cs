using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class ExploreItemsTool : LlmTool<ExploreItemsArguments>
    {
        public override string Name => "explore_items";

        public override string Description =>
            @"
Use this tool to examine the item by its ID.
This tool displays a detailed description of the item, including its effects (satiation, healing, etc.).
You can process any number of IDs at once.
";

        protected override void HandleAttached()
        {
        }

        protected override string Execute(ExploreItemsArguments arguments, LlmCallContext context)
        {
            if (arguments.ItemIds == null || arguments.ItemIds.Length == 0) return "You didn't pass a single ID.";

            StringBuilder result = new();

            foreach (string itemId in arguments.ItemIds)
            {
                ItemAsset item = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(itemId);
                if (item == null)
                {
                    result.AppendLine($"{itemId} does not exist");
                    continue;
                }

                result.AppendLine($"{itemId} : {item.PromptDescription}");
            }

            return result.ToString();
        }
    }
}
