using System;
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
Craft an item by the exact id of a craft you know, see list_crafts.
The ingredients are taken from your bag, the result goes into your bag.
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
            if (string.IsNullOrEmpty(arguments.Craft)) return "Nothing to craft";

            Craft craft = crafter.Known(arguments.Craft);
            if (craft == null)
            {
                return $"You don't know the craft {arguments.Craft}, see list_crafts";
            }

            if (!crafter.TryCraft(craft))
            {
                return $"Failed to craft {arguments.Craft}, make sure you have all the ingredients in your bag";
            }

            return $"Crafted {craft.Key}";
        }
    }
}
