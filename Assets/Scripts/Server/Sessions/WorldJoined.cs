using System;

namespace Shooter.Server.Sessions
{
    public class WorldJoined
    {
        public string WorldId { get; set; }
        public Guid You { get; set; }
    }
}
