namespace Shooter.Game.Llm
{
    public class LlmAnswer
    {
        public string Reply { get; set; }
        public string Compact { get; set; }
        public string Memory { get; set; }
        public LlmInterNpcInteractionCommand[] InterNpcInteractions { get; set; }
        public LlmCharacterRelationCommand[] CharacterRelations { get; set; }

        public class LlmInterNpcInteractionCommand
        {
            public long[] TargetIds { get; set; }
            public string Content { get; set; }
        }

        public class LlmCharacterRelationCommand
        {
            public long TargetId { get; set; }
            public int NewAmount { get; set; }
            public string Reason { get; set; }
        }

        public static LlmAnswer Example()
        {
            return new LlmAnswer()
            {
                Reply = "Your answer to the WANDERER, or null if the system did not explicitly request filling this field.",
                Compact = "A retelling of the conversation with the wanderer, or null if the system did not explicitly request consolidation.",
                Memory = "The new FULL version of your Memory, or null to keep it unchanged",
                InterNpcInteractions = new LlmInterNpcInteractionCommand[]
                {
                    new LlmInterNpcInteractionCommand()
                    {
                        TargetIds = new long[] { 0, 1 },
                        Content = "The message"
                    },
                },
                CharacterRelations = new LlmCharacterRelationCommand[]
                {
                    new LlmCharacterRelationCommand()
                    {
                        TargetId = 0,
                        NewAmount = 100,
                        Reason = "Reason for the relation change"
                    }
                }
            };
        }
    }

}
