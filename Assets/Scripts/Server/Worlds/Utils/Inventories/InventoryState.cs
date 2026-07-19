using System.Collections.Generic;
using Shooter.Server.Worlds.Utils.Items;

namespace Shooter.Server.Worlds.Utils.Inventories
{
    public class InventoryState
    {
        public Dictionary<StackableItem, int> Stacks { get; set; }
        public List<UniqueItemState> Unique { get; set; }
    }
}
