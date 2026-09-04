using System;
using Shooter.Game.Core;
using Shooter.Logging;

namespace Shooter.Game.Llm.LookAtWanderer
{
    [Serializable]
    public class LookAtWandererTool : LlmTool<LookAtWandererArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmPendingTable table;

        public override string Name => "look_at_wanderer";

        public override string Description =>
            @"
Look at wanderer who is talking to you: their health, stamina, belongings, etc.
ALWAYS use this tool when a wanderer starts a conversation with you.
";

        public override bool Available => table.Any;

        public override void OnStart(LlmInitContext context)
        {
            table = context.Self.GetComponent<LlmPendingTable>();
            if (table == null)
            {
                Log.Error($"Entity {context.Self.name} does not have llm pending table component required by tool {Name}");
            }
        }

        protected override string Execute(LookAtWandererArguments arguments, LlmCallContext context)
        {
            long wandererId = arguments.WandererId;
            if (!table.Has(wandererId)) return $"Wanderer {wandererId} isn't talking to you right now.";

            Character wanderer = Character.Of(wandererId, Inactive.Exclude);
            if (wanderer == null)
            {
                Log.Warn($"Unregistered wanderer {wandererId} is waiting for an answer from {context.Self.name}!");
                throw new ArgumentException($"Failed to find wanderer {wandererId}");
            }

            return $"Wanderer {wandererId} state:\n" + Digester.Current.Of(wanderer.gameObject, DigestionDetail.Full);
        }
    }
}
