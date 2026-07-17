using Shooter.Serialization;

namespace Shooter.Server.Sessions
{
    public class Welcome : Serializable
    {
        public long playerId;
        public int tickRate;
    }
}
