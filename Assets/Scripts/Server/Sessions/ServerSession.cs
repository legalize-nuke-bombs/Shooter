using Shooter.Server.Entities.Players;

namespace Shooter.Server.Sessions
{
    public class ServerSession
    {
        public int ConnId { get; }
        public Player Player { get; }
        public string WorldId { get; }
        public bool InWorld { get; set; }

        public ServerSession(int connId, Player player, string worldId)
        {
            ConnId = connId;
            Player = player;
            WorldId = worldId;
        }
    }
}
