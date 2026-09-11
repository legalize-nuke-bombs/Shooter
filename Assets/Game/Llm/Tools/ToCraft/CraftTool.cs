using System;
using System.Text;
using Shooter.Game.Crafting;
using Shooter.Logging;

namespace Shooter.Game.Llm.ToCraft
{
    [Serializable]
    public class CraftTool : LlmTool<CraftArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private Crafter crafter;

        public override string Name => "craft";

        public override string Description =>
            @"
Use this tool to create items.
Pass a dictionary of ratios as an argument, mapping the exact recipe name to the quantity.
";

        protected override void OnStart()
        {
            crafter = Self.GetComponent<Crafter>();
            if (crafter == null)
            {
                Log.Error($"Entity {Self.name} does not have crafter component required by tool {Name}");
            }
        }

        protected override string Execute(CraftArguments arguments, LlmCallContext context)
        {
            if (arguments.Crafts.Length == 0) return "Nothing to craft";

            var sb = new StringBuilder();

            foreach (CraftDto craftDto in arguments.Crafts)
            {
                string craftId = craftDto.CraftName;
                int craftAmount = craftDto.CraftAmount;
                if (craftAmount <= 0)
                {
                    sb.AppendLine($"Bad craft amount ({craftAmount}) for craft {craftId}, craft amount must be positive.");
                    continue;
                }
                Craft craft = crafter.Known(craftId);
                if (craft == null)
                {
                    sb.AppendLine($"You don't know the craft {craftId}, see list_crafts.");
                    continue;
                }

                for (int j = 0; j < craftAmount; j++)
                {
                    if (crafter.TryCraft(craft))
                    {
                        sb.AppendLine($"Crafted {craftId}");
                    }
                    else
                    {
                        sb.AppendLine($"Failed to craft {craftId}, make sure you have all the ingredients in your bag");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
