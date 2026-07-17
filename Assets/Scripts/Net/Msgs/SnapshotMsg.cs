using Shooter.Entities.Characters;
using Shooter.Entities.Chronology;

namespace Shooter.Net.Msgs
{
    public class SnapshotMsg : Msg
    {
        public long tick;
        public PlayerState[] players;
        public ClockState clock;
    }
}
