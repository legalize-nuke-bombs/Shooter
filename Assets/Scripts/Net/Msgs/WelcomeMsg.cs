using System;

namespace Shooter.Net.Msgs
{
    [Serializable]
    public class WelcomeMsg
    {
        public string type;
        public long playerId;
        public int tickRate;
    }
}
