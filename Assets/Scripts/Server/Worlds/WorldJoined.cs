using Shooter.Server.Entities.Players;

namespace Shooter.Server.Worlds
{
    public class WorldJoined
    {
        public string WorldId { get; set; }
        public PlayerState[] Players { get; set; }
    }
}
