using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [Serializable]
    public class ClearHeadTool : LlmTool<ClearHeadArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmHistory llmHistory;
        private LlmNotes llmNotes;

        public override string Name => "clear_head";

        public override string Description =>
            @"
Clear your head. Your notes stay with you; everything else is gone for good.
Call this only when you are told that your head is too full, and only once your notes are complete.
";

        public override bool IsAvailable => llmHistory.IsOverflowing;

        protected override void Start()
        {
            llmHistory = Self.GetComponent<LlmHistory>();
            llmNotes = Self.GetComponent<LlmNotes>();
            if (llmHistory == null)
            {
                Log.Error($"Entity {Self.name} does not have llm history component required by tool {Name}");
            }
            if (llmNotes == null)
            {
                Log.Error($"Entity {Self.name} does not have llm notes component required by tool {Name}");
            }
        }

        protected override string Execute(ClearHeadArguments arguments, LlmCallContext context)
        {
            llmHistory.ForgetMessagesBefore(llmHistory.FindLastAssistantMessageIndex());
            return "You have just cleared your head. Your story so far is gone; everything you chose to keep is written in your notes, and your notes are your memory now. Read the ones you need before you act or speak - start with the one about who you are.\nYour notes:\n" + llmNotes.ListNotes();
        }
    }
}
