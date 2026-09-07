namespace Shooter.Game.Llm.Follow
{
    public class FollowArguments
    {
        public long TargetId { get; set; }
        public int Distance { get; set; }
        public bool Sprint { get; set; }
        public bool Force { get; set; }
    }
}
