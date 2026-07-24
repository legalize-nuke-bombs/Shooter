using System;
using System.Collections.Generic;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Sleeping;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Protocol
{
    public class Snapshot : ClientBound
    {
        public long Tick { get; set; }
        public ClockState Clock { get; set; }
        public SleepState Sleep { get; set; }
        public Dictionary<Guid, EntityState> Entities { get; set; }
    }
}
