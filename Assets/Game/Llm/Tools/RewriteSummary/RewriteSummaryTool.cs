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
            "Replace the story of your life so far with its full retelling.";

        public override bool Available => history.Overflowing;
        public override bool Compacting => true;
        public override LlmLevel Level => LlmLevel.Max;

        protected override string Execute(RewriteSummaryArguments arguments)
        {
            if (string.IsNullOrEmpty(arguments.Text)) return "Nothing to retell";

            history.Retell("THE STORY OF YOUR LIFE SO FAR:\n" + arguments.Text);
            return "Rewritten";
        }
    }
}
