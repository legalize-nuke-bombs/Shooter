using System;
using Shooter.Entities.Characters;
using Shooter.Entities.Chronology;

namespace Shooter.Net.Msgs
{
    [Serializable]
    public class SnapshotMsg
    {
        public string type;
        public long tick;
        public PlayerState[] players;
        public ClockState clock;
    }
}
