using System.Collections.Generic;
using Shooter.Server.Worlds.Utils.Items;

namespace Shooter.Server.Worlds.Utils.Inventories
{
    public class InventoryState
    {
        public Dictionary<StackableItem, int> Stacks { get; set; }
        public Dictionary<long, UniqueItemState> Unique { get; set; }
        public long? EquiptedId { get; set; }
    }
}
