using System;
using Shooter.Game.Core;
using Shooter.Game.Llm.GiveStackable;
using Shooter.Game.Loot;
using Shooter.Logging;

namespace Shooter.Game.Llm.UseItem
{
    [Serializable]
    public sealed class UseItemTool : LlmTool<UseItemArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private Inventory inventory;

        public override string Name => "use_item";

        public override string Description =>
            @$"
Use (consume, etc) some of your stackable item.
The item is addressed by its exact name from your bag.
";

        protected override void OnStart()
        {
            inventory = Self.GetComponent<Inventory>();
            if (inventory == null)
            {
                Log.Error($"Entity {Self.name} does not have inventory component required by tool {Name}");
            }
        }

        protected override string Execute(UseItemArguments arguments, LlmCallContext context)
        {
            if (inventory.UseStackable(arguments.Item))
            {
                return $"{arguments.Item} was used";
            }
            return $"Failed to use {arguments.Item}, make sure you have that item in your inventory and that it can be used.";
        }
    }
}
