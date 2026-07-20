using Shooter.Server.Worlds.Utils.Inventories;

namespace Shooter.Server.Worlds.Entities.Npcs
{
    public class NpcState
    {
        public long Id { get; set; }

        public string Name { get; set; }
        public bool Alive { get; set; }
        public InventoryState InventoryState { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
    }
}
