using System.Collections.Generic;
using Shooter.Server.Worlds.Entities.Players;

namespace Shooter.Server.Sessions
{
    public class WorldJoined
    {
        public string WorldId { get; set; }
        public List<PlayerState> Players { get; set; }
    }
}
