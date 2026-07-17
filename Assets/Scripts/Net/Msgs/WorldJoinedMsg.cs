using System;
using Shooter.Entities.Characters;

namespace Shooter.Net.Msgs
{
    [Serializable]
    public class WorldJoinedMsg
    {
        public string type;
        public string worldId;
        public PlayerState[] players;
    }
}
