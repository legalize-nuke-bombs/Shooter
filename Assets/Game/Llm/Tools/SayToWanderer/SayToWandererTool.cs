using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Llm))]
    public sealed class SayToWandererTool : LlmTool<SayToWandererArguments>
    {
        private Llm llm;
        private LlmPendingTable table;

        public override string Name => "say_to_wanderer";

        public override string Description =>
            "Answer a wanderer who is talking to you. Answer in the language the wanderer speaks.";

        public override bool Available => table.Any;

        protected override void Awake()
        {
            base.Awake();
            llm = GetComponent<Llm>();
            table = GetComponent<LlmPendingTable>();
        }

        protected override string Execute(SayToWandererArguments arguments, LlmCallContext context)
        {
            if (string.IsNullOrEmpty(arguments.Text)) return "Nothing to say";

            return llm.Answer(arguments.WandererId, arguments.Text)
                ? $"Said to {arguments.WandererId}"
                : $"No wanderer {arguments.WandererId} is waiting for your answer";
        }
    }
}
