using Shooter.Server.Entities.Players;
using Shooter.Server.Entities.Chronology;
using Shooter.Server.Entities.Npcs;

namespace Shooter.Server.Worlds
{
    public class Snapshot
    {
        public long Tick { get; set; }
        public ClockState Clock { get; set; }
        public PlayerState[] Players { get; set; }
        public NpcState[] Npcs { get; set; }
    }
}
