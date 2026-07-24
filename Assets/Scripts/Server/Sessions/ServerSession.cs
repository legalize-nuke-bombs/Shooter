namespace Shooter.Server.Sessions
{
    public class ServerSession
    {
        public int ConnId { get; }
        public long UserId { get; }
        public string WorldId { get; }
        public string DisplayName { get; }
        public bool InWorld { get; set; }

        public ServerSession(int connId, long userId, string worldId, string displayName)
        {
            ConnId = connId;
            UserId = userId;
            WorldId = worldId;
            DisplayName = string.IsNullOrEmpty(displayName) ? "player" + userId : displayName;
        }
    }
}
