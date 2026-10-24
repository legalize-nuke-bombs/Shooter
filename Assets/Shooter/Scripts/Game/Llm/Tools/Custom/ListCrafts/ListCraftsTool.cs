using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class ListCraftsTool : LlmTool<ListCraftsArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private Crafter crafter;

        public override string Name => "list_crafts";

        public override string Description =>
            @"
List the crafts you know: the id of each craft, its ingredients and its result.
";

        protected override void Start()
        {
            crafter = Self.GetComponent<Crafter>();
            if (crafter == null)
            {
                Log.Error($"Entity {Self.name} does not have crafter component required by tool {Name}");
            }
        }

        protected override string Execute(ListCraftsArguments arguments, LlmCallContext context)
        {
            List<CraftAsset> crafts = crafter.AvailableCrafts;
            if (crafts.Count == 0)
            {
                return "You don't know any crafts";
            }

            StringBuilder result = new();
            foreach (CraftAsset craft in crafts)
            {
                result.AppendLine(craft.DescribeForPrompt());
            }
            return result.ToString();
        }
    }
}
