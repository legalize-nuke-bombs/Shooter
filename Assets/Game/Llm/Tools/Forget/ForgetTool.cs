using Shooter.Game.Llm.Notes;
using UnityEngine;

namespace Shooter.Game.Llm.Forget
{
    [RequireComponent(typeof(LlmHistory))]
    [RequireComponent(typeof(LlmNotes))]
    public class ForgetTool : LlmTool<ForgetArguments>
    {
        private LlmHistory history;
        private LlmNotes notes;

        public override string Name => "forget";

        public override string Description =>
            @"
Erase your whole story. Everything not saved in your notes is lost FOREVER.
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

        protected override string Execute(ForgetArguments arguments, LlmCallContext context)
        {
            history.Forget(context.PromptedCount);
            return "Erased. Your past now exists only in your notes. Before you act or speak, read the ones you need - start with who you are.\nYour notes:\n" + notes.List();
        }
    }
}
