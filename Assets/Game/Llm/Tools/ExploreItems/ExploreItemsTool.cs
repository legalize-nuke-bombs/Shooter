using System.Reflection.Metadata.Ecma335;
using System.Text;
using Shooter.Game.Loot;
using Shooter.Game.World;

namespace Shooter.Game.Llm.ExploreItems
{
    public class ExploreItemsTool : LlmTool<ExploreItemsArguments>
    {
        public override string Name => "explore_items";

        public override string Description =>
            @"
Use this tool to get detailed item descriptions based on their IDs.
You can process any number of IDs at once.
";

        protected override string Execute(ExploreItemsArguments arguments)
        {
            if (arguments.ItemIds == null || arguments.ItemIds.Length == 0)
            {
                return "You didn't pass a single ID.";
            }

            var sb = new StringBuilder();

            foreach (string id in arguments.ItemIds)
            {
                ItemSpec item = Environment.Current.Items.Of(id);
                if (item == null)
                {
                    sb.AppendLine($"{id} does not exist");
                    continue;
                }

                sb.AppendLine($"{id} : {item.PromptDescription}");
            }

            return sb.ToString();
        }
    }
}
