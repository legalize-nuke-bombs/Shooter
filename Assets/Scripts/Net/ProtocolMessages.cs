using System;
using Shooter.Entities.Player;
using Shooter.Entities.Chronology;

namespace Shooter.Net
{
    [Serializable]
    public class HelloMsg
    {
        public string type = "hello";
        public string name;
    }

    [Serializable]
    public class WelcomeMsg
    {
        public string type;
        public long playerId;
        public int tickRate;
    }

    [Serializable]
    public class JoinWorldMsg
    {
        public string type = "joinWorld";
    }

    [Serializable]
    public class WorldJoinedMsg
    {
        public string type;
        public string worldId;
        public PlayerStateMsg[] players;
    }

    [Serializable]
    public class JoinedMsg
    {
        public string type;
        public long id;
        public string name;
    }

    [Serializable]
    public class LeftMsg
    {
        public string type;
        public long id;
    }

    [Serializable]
    public class SnapshotMsg
    {
        public string type;
        public long tick;
        public PlayerStateMsg[] players;
        public ClockState clock;
    }
}
