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
