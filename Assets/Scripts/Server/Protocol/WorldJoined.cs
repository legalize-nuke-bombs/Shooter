using System;
using Shooter.Client;

namespace Shooter.Server.Protocol
{
    public class WorldJoined : ClientBound
    {
        public string WorldId { get; set; }
        public Guid You { get; set; }

        public override void Apply(ClientHost host)
        {
            host.OnWorldJoined(this);
        }
    }
}
