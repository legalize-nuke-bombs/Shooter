using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public sealed class UseItemTool : LlmTool<UseItemArguments>
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private InventoryUser user;

        public override string Name => "use_item";

        public override string Description =>
            @$"
Use (consume, etc) some of your stackable item.
The item is addressed by its exact name from your bag.
";

        protected override void OnStart()
        {
            user = Self.GetComponent<InventoryUser>();
            if (user == null)
            {
                Log.Error($"Entity {Self.name} does not have inventory user component required by tool {Name}");
            }
        }

        protected override string Execute(UseItemArguments arguments, LlmCallContext context)
        {
            if (Catalogs.GetCatalog<ItemCatalog>().GetAssetById(arguments.Item) is StackableItemAsset item && user.TryUseItem(item))
            {
                return $"{arguments.Item} was used";
            }
            return $"Failed to use {arguments.Item}, make sure you have that item in your inventory and that it can be used.";
        }
    }
}
