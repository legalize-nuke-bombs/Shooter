namespace Shooter.Server.Sessions
{
    public class SessionGrant
    {
        public string WorldId { get; }
        public string DisplayName { get; }
        public long ExpiresAt { get; }

        public SessionGrant(string worldId, string displayName, long expiresAt)
        {
            WorldId = worldId;
            DisplayName = displayName;
            ExpiresAt = expiresAt;
        }
    }
}
