using Shooter.Server.Characters;
using Shooter.Server.Chronology;

namespace Shooter.Net.Msgs
{
    public class SnapshotMsg : Msg
    {
        public long tick;
        public PlayerState[] players;
        public ClockState clock;
    }
}
