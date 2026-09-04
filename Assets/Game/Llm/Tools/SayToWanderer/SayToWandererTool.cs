using System;
using Shooter.Logging;

namespace Shooter.Game.Llm.SayToWanderer
{
    [Serializable]
    public sealed class SayToWandererTool : LlmTool<SayToWandererArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private Llm llm;
        private LlmPendingTable table;

        public override string Name => "say_to_wanderer";

        public override string Description =>
            "Answer a wanderer who is talking to you. Answer in the language the wanderer speaks.";

        public override bool Available => table.Any;

        protected override void OnStart()
        {
            llm = Self.GetComponent<Llm>();
            if (llm == null)
            {
                Log.Error($"Entity {Self.name} does not have Llm component required by tool {Name}");
            }
            table = Self.GetComponent<LlmPendingTable>();
            if (table == null)
            {
                Log.Error($"Entity {Self.name} does not have LlmPendingTable component required by tool {Name}");
            }
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
