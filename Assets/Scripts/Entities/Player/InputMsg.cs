using System;

namespace Shooter.Entities.Player
{
    [Serializable]
    public class InputMsg
    {
        public string type = "input";
        public int seq;
        public float moveX;
        public float moveZ;
        public bool jump;
        public bool sprint;
        public float yaw;
        public float pitch;
    }
}
