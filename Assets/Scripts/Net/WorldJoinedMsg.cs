using System;
using Shooter.Entities.Player;

namespace Shooter.Net
{
    [Serializable]
    public class WorldJoinedMsg
    {
        public string type;
        public string worldId;
        public PlayerStateMsg[] players;
    }
}
