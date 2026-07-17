using System;

namespace Shooter.Net.Msgs
{
    [Serializable]
    public class InputMsg
    {
        public string type = "input";
        public float moveX;
        public float moveZ;
        public bool jump;
        public bool sprint;
        public float yaw;
        public float pitch;
    }
}
