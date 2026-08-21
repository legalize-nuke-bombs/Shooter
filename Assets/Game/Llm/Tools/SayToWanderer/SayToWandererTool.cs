using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(LlmWaiting))]
    public sealed class SayToWandererTool : LlmTool<SayToWandererArguments>
    {
        private LlmWaiting waiting;

        public override string Name => "say_to_wanderer";

        public override string Description =>
            "Answer a wanderer who is talking to you. Answer in the language the wanderer speaks.";

        public override bool Available => waiting.Any;

        protected override void Awake()
        {
            base.Awake();
            waiting = GetComponent<LlmWaiting>();
        }

        protected override string Execute(SayToWandererArguments arguments, LlmCallContext context)
        {
            if (string.IsNullOrEmpty(arguments.Text)) return "Nothing to say";

            return waiting.Answer(arguments.WandererId, arguments.Text)
                ? $"Said to {arguments.WandererId}"
                : $"No wanderer {arguments.WandererId} is waiting for your answer";
        }
    }
}
