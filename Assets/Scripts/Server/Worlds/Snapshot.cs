using Shooter.Server.Entities.Players;
using Shooter.Server.Entities.Chronology;

namespace Shooter.Server.Worlds
{
    public class Snapshot
    {
        public long Tick { get; set; }
        public PlayerState[] Players { get; set; }
        public ClockState Clock { get; set; }
    }
}
