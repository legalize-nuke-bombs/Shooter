using Shooter.Serialization;
using Shooter.Server.Characters;
using Shooter.Server.Chronology;

namespace Shooter.Server.Session
{
    public class SnapshotMsg : Serializable
    {
        public long tick;
        public PlayerState[] players;
        public ClockState clock;
    }
}
