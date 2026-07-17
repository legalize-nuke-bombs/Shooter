using Shooter.Serialization;
using Shooter.Server.Entities.Characters.Player;
using Shooter.Server.Entities.Chronology;

namespace Shooter.Server.Worlds
{
    public class Snapshot : Serializable
    {
        public long tick;
        public PlayerState[] players;
        public ClockState clock;
    }
}
