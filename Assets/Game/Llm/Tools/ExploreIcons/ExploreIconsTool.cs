using System.Text;
using Shooter.Game.Core;
using Shooter.Game.World;

namespace Shooter.Game.Llm.ExploreIcons
{
    public sealed class ExploreIconsTool : LlmTool<ExploreIconsArguments>
    {
        public override string Name => "explore_icons";

        public override string Description =>
            "Get a list of the available icons";

        protected override string Execute(ExploreIconsArguments arguments)
        {
            var sb = new StringBuilder();

            IconCatalog catalog = Environment.Current.Icons;
            for (int i = 0; i < catalog.Count; i++)
            {
                IconSpec spec = catalog.At(i);
                sb.AppendLine(spec.Id + ": " + spec.PromptDescription);
            }

            return sb.ToString();
        }
    }
}
