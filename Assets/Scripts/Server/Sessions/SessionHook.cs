namespace Shooter.Server.Sessions
{
    public class SessionHook
    {
        public SessionHookAction Action { get; set; }
        public long? UserId { get; set; }
        public string WorldId { get; set; }
        public string DisplayName { get; set; }
    }
}
