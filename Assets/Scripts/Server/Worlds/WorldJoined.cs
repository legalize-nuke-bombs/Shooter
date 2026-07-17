using Shooter.Serialization;
using Shooter.Server.Entities.Characters.Player;

namespace Shooter.Server.Worlds
{
    public class WorldJoined : Serializable
    {
        public string worldId;
        public PlayerState[] players;
    }
}
