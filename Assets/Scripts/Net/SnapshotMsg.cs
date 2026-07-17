using System;
using Shooter.Entities.Player;
using Shooter.Entities.Chronology;

namespace Shooter.Net
{
    [Serializable]
    public class SnapshotMsg
    {
        public string type;
        public long tick;
        public PlayerStateMsg[] players;
        public ClockState clock;
    }
}
