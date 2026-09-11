using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Crafting;
using Shooter.Game.Loot;
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
Craft an item using its recipe.
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
            ItemSpec result = crafter.TryCraft(arguments.Recipe.ToArray());
            if (result == null)
            {
                return "Failed to craft";
            }
            return $"Successfully crafted {result.Id}";
        }
    }
}
