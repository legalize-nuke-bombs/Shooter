using Shooter.Serialization;

namespace Shooter.Server.Session
{
    public class WelcomeMsg : Serializable
    {
        public long playerId;
        public int tickRate;
    }
}
