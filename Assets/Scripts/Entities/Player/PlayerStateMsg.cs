using System;

namespace Shooter.Entities.Player
{
    [Serializable]
    public class PlayerStateMsg
    {
        public long id;
        public string name;
        public float x;
        public float y;
        public float z;
        public float yaw;
        public float pitch;
    }
}
