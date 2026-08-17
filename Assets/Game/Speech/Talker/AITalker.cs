using System;
using Shooter.Logging;
using Shooter.Game.Core;

namespace Shooter.Game.Speech
{
    public sealed class AITalker : Talker
    {
        private static readonly Journal Log = Logs.Here();
        private const string Fallback = "Not now.";

        private Llm.Llm llm;

        private void Awake()
        {
            llm = this.Find<Llm.Llm>();
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
