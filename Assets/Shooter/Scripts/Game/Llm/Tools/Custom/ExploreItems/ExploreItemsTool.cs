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
Use this tool to examine items by their IDs.
This tool displays a detailed description of specified items, including their effects (satiation, healing, etc.) and market value.
Always use this tool when you’re not sure about items.
";

        protected override void Start()
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
