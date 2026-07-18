using System.Collections.Generic;
using Shooter.Server.Entities.Players;

namespace Shooter.Server.Worlds
{
    public class WorldJoined
    {
        public string WorldId { get; set; }
        public List<PlayerState> Players { get; set; }
    }
}
