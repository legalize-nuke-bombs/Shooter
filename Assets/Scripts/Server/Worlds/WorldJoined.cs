using Shooter.Serialization;
using Shooter.Server.Entities.Players;

namespace Shooter.Server.Worlds
{
    public class WorldJoined : Serializable
    {
        public string worldId;
        public PlayerState[] players;
    }
}
