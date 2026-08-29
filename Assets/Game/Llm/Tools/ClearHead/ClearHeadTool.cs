using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.ClearHead
{
    [RequireComponent(typeof(LlmHistory))]
    [RequireComponent(typeof(LlmNotes))]
    public class ClearHeadTool : LlmTool<ClearHeadArguments>
    {
        private LlmHistory history;
        private LlmNotes notes;

        public override string Name => "clear_head";

        public override string Description =>
            @"
Clear your head. Your notes stay with you; everything else is gone for good.
Call this only when your notes are complete.
";

        public override bool Available => history.Overflowing;
        public override LlmLevel Level => LlmLevel.Max;

        protected override void Awake()
        {
            base.Awake();
            history = GetComponent<LlmHistory>();
            notes = GetComponent<LlmNotes>();
        }

        protected override string Execute(ClearHeadArguments arguments, LlmCallContext context)
        {
            history.Forget(history.LastTurn());
            return "You have just cleared your head. Your story so far is gone; everything you chose to keep is written in your notes, and your notes are your memory now. Read the ones you need before you act or speak - start with the one about who you are.\nYour notes:\n" + notes.List();
        }
    }
}
