using System;
using System.Text;
using Shooter.Infrastructure;

namespace Shooter.Game
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

        protected override void HandleAttached()
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

            StringBuilder result = new();

            foreach (CraftDto craftDto in arguments.Crafts)
            {
                string craftId = craftDto.CraftName;
                int craftAmount = craftDto.CraftAmount;
                if (craftAmount <= 0)
                {
                    result.AppendLine($"Bad craft amount ({craftAmount}) for craft {craftId}, craft amount must be positive.");
                    continue;
                }
                CraftAsset craft = crafter.FindKnownCraft(craftId);
                if (craft == null)
                {
                    result.AppendLine($"You don't know the craft {craftId}, see list_crafts.");
                    continue;
                }

                for (int i = 0; i < craftAmount; i++)
                {
                    if (crafter.TryCraft(craft))
                    {
                        result.AppendLine($"Crafted {craftId}");
                    }
                    else
                    {
                        result.AppendLine($"Failed to craft {craftId}, make sure you have all the ingredients in your bag");
                    }
                }
            }

            return result.ToString();
        }
    }
}
