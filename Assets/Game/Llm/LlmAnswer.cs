namespace Shooter.Game.Llm
{
    public class LlmAnswer
    {
        public string Reply { get; set; }
        public string Compact { get; set; }
        public string Memory { get; set; }
        public InterNpcInteractionCommand[] InterNpcInteractions { get; set; }
        public CharacterRelationCommand[] CharacterRelations { get; set; }
        public GiveStackableItemCommand[] GiveStackableItems { get; set; }
        public GiveUniqueItemCommand[] GiveUniqueItems { get; set; }

        public class InterNpcInteractionCommand
        {
            public long[] TargetIds { get; set; }
            public string Content { get; set; }
        }

        public class CharacterRelationCommand
        {
            public long TargetId { get; set; }
            public int NewAmount { get; set; }
            public string Reason { get; set; }
        }

        public class GiveStackableItemCommand
        {
            public long TargetId { get; set; }
            public string ItemName { get; set; }
            public int ItemAmount { get; set; }
        }

        public class GiveUniqueItemCommand
        {
            public long TargetId { get; set; }
            public int SlotIdx { get; set; }
        }

        public static LlmAnswer Example()
        {
            return new LlmAnswer()
            {
                Reply = "Your answer to the WANDERER, or null if the system did not explicitly request filling this field.",
                Compact = "A retelling of the conversation with the wanderer, or null if the system did not explicitly request consolidation.",
                Memory = "The new FULL version of your Memory, or null to keep it unchanged",
                InterNpcInteractions = new InterNpcInteractionCommand[]
                {
                    new InterNpcInteractionCommand()
                    {
                        TargetIds = new long[] { 0, 1 },
                        Content = "The message"
                    },
                },
                CharacterRelations = new CharacterRelationCommand[]
                {
                    new CharacterRelationCommand()
                    {
                        TargetId = 0,
                        NewAmount = 100,
                        Reason = "Reason for the relation change"
                    }
                },
                GiveStackableItems = new GiveStackableItemCommand[]
                {
                    new GiveStackableItemCommand()
                    {
                        TargetId = 0,
                        ItemName = "The exact STACKABLE item name",
                        ItemAmount = 100
                    }
                },
                GiveUniqueItems = new GiveUniqueItemCommand[]
                {
                    new GiveUniqueItemCommand()
                    {
                        TargetId = 0,
                        SlotIdx = 0
                    }
                }
            };
        }
    }

}
