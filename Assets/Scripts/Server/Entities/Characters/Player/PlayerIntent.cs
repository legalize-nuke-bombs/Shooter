using Shooter.Serialization;

namespace Shooter.Server.Entities.Characters.Player
{
    public class PlayerIntent : Serializable
    {
        public float moveX;
        public float moveZ;
        public bool jump;
        public bool sprint;
        public float yaw;
        public float pitch;
    }
}
