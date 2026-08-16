using System;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Llm.LookAtWanderer
{
    [RequireComponent(typeof(LlmWaiting))]
    [RequireComponent(typeof(Digester))]
    public class LookAtWandererTool : LlmTool<LookAtWandererArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private LlmWaiting waiting;
        private Digester digester;

        protected override void Awake()
        {
            base.Awake();
            waiting = GetComponent<LlmWaiting>();
            digester = GetComponent<Digester>();
        }

        public override string Name => "look_at_wanderer";

        public override string Description =>
            @"
Look at wanderer who is talking to you: their health, stamina, belongings, etc.
ALWAYS use this tool when a wanderer starts a conversation with you.
";

        public override bool Available => waiting.Any;

        protected override string Execute(LookAtWandererArguments arguments)
        {
            long wandererId = arguments.WandererId;
            if (!waiting.IsWaiting(wandererId))
            {
                return $"Wanderer {wandererId} isn't talking to you right now.";
            }

            Register<PersistentId> ids = Environment.Current.Registers.Of<PersistentId>();
            PersistentId wanderer = ids.Of(wandererId);
            if (wanderer == null)
            {
                Log.Warn($"Unregistered wanderer {wandererId} is waiting for an answer from {name}!");
                throw new ArgumentException($"Failed to find wanderer {wandererId}");
            }

            return $"Wanderer {wandererId} state:\n" + digester.Of(wanderer.gameObject, DigestionDetail.Full);
        }
    }
}
