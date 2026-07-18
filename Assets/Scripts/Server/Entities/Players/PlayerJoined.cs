using Shooter.Serialization;

namespace Shooter.Server.Entities.Players
{
    public class PlayerJoined : Serializable
    {
        public long id;
        public string name;
    }
}
