using System;

namespace Shooter.Server.Protocol
{
    public class WorldJoined : ClientBound
    {
        public string WorldId { get; set; }
        public Guid You { get; set; }
    }
}
