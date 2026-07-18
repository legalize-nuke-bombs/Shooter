using System.Collections.Generic;
using Shooter.Server.Worlds.Entities.Players;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds.Entities.Npcs;
using Shooter.Server.Worlds.Entities.Sleeping;

namespace Shooter.Server.Worlds
{
    public class Snapshot
    {
        public long Tick { get; set; }
        public ClockState Clock { get; set; }
        public List<PlayerState> Players { get; set; }
        public List<NpcState> Npcs { get; set; }
        public SleepState Sleep { get; set; }
    }
}
