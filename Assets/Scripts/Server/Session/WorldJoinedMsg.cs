using Shooter.Serialization;
using Shooter.Server.Characters;

namespace Shooter.Server.Session
{
    public class WorldJoinedMsg : Serializable
    {
        public string worldId;
        public PlayerState[] players;
    }
}
