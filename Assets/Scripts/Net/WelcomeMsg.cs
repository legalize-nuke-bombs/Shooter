using System;

namespace Shooter.Net
{
    [Serializable]
    public class WelcomeMsg
    {
        public string type;
        public long playerId;
        public int tickRate;
    }
}
