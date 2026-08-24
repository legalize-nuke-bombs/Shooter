namespace Shooter.Game.Llm
{
    public class SendMessageArguments
    {
        public long[] TargetIds { get; set; }
        public bool Urgent { get; set; }
        public string Content { get; set; }
    }
}
