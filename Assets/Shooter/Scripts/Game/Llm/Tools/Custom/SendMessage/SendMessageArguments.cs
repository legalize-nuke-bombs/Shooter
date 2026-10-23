namespace Shooter.Game
{
    public class SendMessageArguments
    {
        public long[] TargetIds { get; set; }
        public bool Urgent { get; set; }
        public string Content { get; set; }
    }
}
