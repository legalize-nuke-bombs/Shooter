namespace Shooter.Game.Llm
{
    public class LlmAnswer
    {
        public string Reply { get; set; }
        public string Memory { get; set; }
        public LlmInterNpcInteractionCommand[] InterNpcInteractions { get; set; }

        public class LlmInterNpcInteractionCommand
        {
            public string[] TargetNames { get; set; }
            public string Content { get; set; }
        }

        public static LlmAnswer Example()
        {
            return new LlmAnswer()
            {
                Reply = "Your answer to the wanderer, or null if nobody is waiting for one",
                Memory = "The new FULL version of your Memory, or null to keep it unchanged",
                InterNpcInteractions = new LlmInterNpcInteractionCommand[]
                {
                    new LlmInterNpcInteractionCommand()
                    {
                        TargetNames = new string[] { "Exact recipient name", "Another recipient of the same message" },
                        Content = "The message"
                    },
                }
            };
        }
    }

}
