using Shooter.Serialization;

namespace Shooter.Server.Entities.Characters.Player
{
    public class PlayerJoined : Serializable
    {
        public long id;
        public string name;
    }
}
