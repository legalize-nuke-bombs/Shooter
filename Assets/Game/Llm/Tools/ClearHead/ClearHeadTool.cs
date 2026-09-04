using System;
using Shooter.Game.Llm.Notes;
using Shooter.Logging;

namespace Shooter.Game.Llm.ClearHead
{
    [Serializable]
    public class ClearHeadTool : LlmTool<ClearHeadArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmHistory history;
        private LlmNotes notes;

        public override string Name => "clear_head";

        public override string Description =>
            @"
Clear your head. Your notes stay with you; everything else is gone for good.
Call this only when your notes are complete.
";

        public override bool Available => history.Overflowing;

        protected override void OnStart()
        {
            history = Self.GetComponent<LlmHistory>();
            notes = Self.GetComponent<LlmNotes>();
            if (history == null)
            {
                Log.Error($"Entity {Self.name} does not have llm history component required by tool {Name}");
            }
            if (notes == null)
            {
                Log.Error($"Entity {Self.name} does not have llm notes component required by tool {Name}");
            }
        }

        protected override string Execute(ClearHeadArguments arguments, LlmCallContext context)
        {
            history.Forget(history.LastTurn());
            return "You have just cleared your head. Your story so far is gone; everything you chose to keep is written in your notes, and your notes are your memory now. Read the ones you need before you act or speak - start with the one about who you are.\nYour notes:\n" + notes.List();
        }
    }
}
