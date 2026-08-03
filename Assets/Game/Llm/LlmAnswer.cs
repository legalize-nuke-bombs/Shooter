namespace Shooter.Game.Llm
{
    public class LlmAnswer
    {
        public string Reply { get; set; }
        public string Memory { get; set; }
        public LlmInterNpcInteractionCommand InterNpcInteraction { get; set; }

        public class LlmInterNpcInteractionCommand
        {
            public string[] TargetNames { get; set; }
            public string Content { get; set; }
        }
    }
}
