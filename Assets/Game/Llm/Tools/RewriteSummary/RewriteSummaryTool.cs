using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(LlmHistory))]
    public sealed class RewriteSummaryTool : LlmTool<RewriteSummaryArguments>
    {
        private LlmHistory history;

        public override string Name => "rewrite_summary";

        public override string Description =>
            "Retell the story of your life. Your call replaces everything older: its text remains as the only story.";

        public override bool Available => history.Overflowing;
        public override LlmLevel Level => LlmLevel.Max;

        protected override void Awake()
        {
            base.Awake();
            history = GetComponent<LlmHistory>();
        }

        protected override string Execute(RewriteSummaryArguments arguments, LlmCallContext context)
        {
            if (string.IsNullOrEmpty(arguments.Text)) return "Nothing to retell";

            history.Forget(context.PromptedCount);
            return "Rewritten";
        }
    }
}
