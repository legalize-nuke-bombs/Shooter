using System;
using System.Collections.Generic;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Time;
using Shooter.Server.Worlds.Sleeping;

namespace Shooter.Server.Worlds
{
    public class Snapshot
    {
        public long Tick { get; set; }
        public ClockState Clock { get; set; }
        public SleepState Sleep { get; set; }
        public Dictionary<Guid, EntityState> Entities { get; set; }
    }
}
