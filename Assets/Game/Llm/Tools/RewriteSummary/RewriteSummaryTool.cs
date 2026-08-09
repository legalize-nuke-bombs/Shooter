using UnityEngine;

namespace Shooter.Game.Llm.Tools
{
    [RequireComponent(typeof(LlmHistory))]
    public sealed class RewriteSummaryTool : LlmTool<RewriteSummaryArguments>
    {
        private LlmHistory history;

        private void Awake()
        {
            history = GetComponent<LlmHistory>();
        }

        public override string Name => "rewrite_summary";

        public override string Description =>
            "Retell the story of your life. Your call replaces everything older: its text remains as the only story.";

        public override bool Available => history.Overflowing;
        public override LlmLevel Level => LlmLevel.Max;

        protected override string Execute(RewriteSummaryArguments arguments)
        {
            if (string.IsNullOrEmpty(arguments.Text)) return "Nothing to retell";

            history.Forget();
            return "Rewritten";
        }
    }
}
