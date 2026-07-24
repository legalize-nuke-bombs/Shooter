namespace Shooter.Server.Protocol
{
    public class Welcome : ClientBound
    {
        public long UserId { get; set; }
        public int TickRate { get; set; }
    }
}
