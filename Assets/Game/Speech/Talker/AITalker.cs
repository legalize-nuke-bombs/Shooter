using System;
using Shooter.Game.Core;
using Shooter.Logging;

namespace Shooter.Game.Speech
{
    public sealed class AITalker : Talker
    {
        private const string Fallback = "Not now.";
        private static readonly Journal Log = Logs.Here();

        private Llm.Llm llm;

        protected override void Awake()
        {
            base.Awake();
            llm = GetComponent<Llm.Llm>();
        }

        protected override void RequestAnswer(long wandererId, string message, Action<string> onAnswer)
        {
            if (llm == null)
            {
                Log.Warn($"Entity {name} has no llm to answer with");
                onAnswer(Fallback);
                return;
            }

            llm.Listen(wandererId, message, onAnswer);
        }

        protected override void Forget(long wandererId)
        {
            if (llm != null) llm.Forget(wandererId);
        }
    }
}
