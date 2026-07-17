using Shooter.Serialization;

namespace Shooter.Server.Characters
{
    public class InputMsg : Serializable
    {
        public float moveX;
        public float moveZ;
        public bool jump;
        public bool sprint;
        public float yaw;
        public float pitch;
    }
}
