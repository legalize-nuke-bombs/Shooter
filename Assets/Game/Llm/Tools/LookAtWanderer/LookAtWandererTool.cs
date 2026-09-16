using System;
using Shooter.Game.Core;
using Shooter.Logging;

namespace Shooter.Game.Llm.LookAtWanderer
{
    [Serializable]
    public class LookAtWandererTool : LlmTool<LookAtWandererArguments>
    {
        private static readonly Journal Log = Logs.Here();

        public override string Name => "look_at_wanderer";

        public override string Description =>
            @"
Look at wanderer who is talking to you: their health, stamina, belongings, etc.
ALWAYS use this tool when a wanderer starts a conversation with you.
";


        protected override void OnStart()
        {
        }

        protected override string Execute(LookAtWandererArguments arguments, LlmCallContext context)
        {
            long wandererId = arguments.WandererId;
            Character wanderer = Character.Of(wandererId, Inactive.Exclude);
            if (wanderer == null)
            {
                Log.Warn($"Unregistered wanderer {wandererId} is waiting for an answer from {Self.name}!");
                throw new ArgumentException($"Failed to find wanderer {wandererId}");
            }

            return $"Wanderer {wandererId} state:\n" + Digester.Current.Of(wanderer.gameObject, DigestionDetail.Full);
        }
    }
}
