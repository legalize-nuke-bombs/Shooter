using Shooter.Entities.Characters;

namespace Shooter.Net.Msgs
{
    public class WorldJoinedMsg : Msg
    {
        public string worldId;
        public PlayerState[] players;
    }
}
