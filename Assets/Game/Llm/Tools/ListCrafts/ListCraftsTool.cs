using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.Crafting;
using Shooter.Game.Llm.ListNotes;
using Shooter.Logging;

namespace Shooter.Game.Llm.ListCrafts
{
    [Serializable]
    public class ListCraftsTool : LlmTool<ListCraftsArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private Crafter crafter;

        public override string Name => "list_crafts";

        public override string Description =>
            @"
Get a list of the crafting recipes you can make.
";

        protected override void OnStart()
        {
            crafter = Self.GetComponent<Crafter>();
            if (crafter == null)
            {
                Log.Error($"Entity {Self.name} does not have crafter component required by tool {Name}");
            }
        }

        protected override string Execute(ListCraftsArguments arguments, LlmCallContext context)
        {
            List<Craft> crafts = crafter.AvailableCrafts;
            if (crafts.Count == 0)
            {
                return "You don't know any crafts";
            }

            var sb = new StringBuilder();
            foreach (Craft craft in crafts)
            {
                sb.AppendLine(craft.PromptDescription());
            }
            return sb.ToString();
        }
    }
}
