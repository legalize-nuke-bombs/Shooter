using System;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm.LookAtWanderer
{
    [RequireComponent(typeof(LlmPendingTable))]
    [RequireComponent(typeof(Digester))]
    public class LookAtWandererTool : LlmTool<LookAtWandererArguments>
    {
        private static readonly Journal Log = Logs.Here();
        private Digester digester;

        private LlmPendingTable table;

        public override string Name => "look_at_wanderer";

        public override string Description =>
            @"
Look at wanderer who is talking to you: their health, stamina, belongings, etc.
ALWAYS use this tool when a wanderer starts a conversation with you.
";

        public override bool Available => table.Any;

        protected override void Awake()
        {
            base.Awake();
            table = GetComponent<LlmPendingTable>();
            digester = GetComponent<Digester>();
        }

        protected override string Execute(LookAtWandererArguments arguments, LlmCallContext context)
        {
            long wandererId = arguments.WandererId;
            if (!table.Has(wandererId)) return $"Wanderer {wandererId} isn't talking to you right now.";

            Character wanderer = Character.Of(wandererId, Inactive.Exclude);
            if (wanderer == null)
            {
                Log.Warn($"Unregistered wanderer {wandererId} is waiting for an answer from {name}!");
                throw new ArgumentException($"Failed to find wanderer {wandererId}");
            }

            return $"Wanderer {wandererId} state:\n" + digester.Of(wanderer.gameObject, DigestionDetail.Full);
        }
    }
}
