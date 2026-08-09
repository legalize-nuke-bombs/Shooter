using System;
using Shooter.Logging;

namespace Shooter.Game.Speech
{
    public sealed class AITalker : Talker
    {
        private static readonly Journal Log = Logs.Here();
        private const string Fallback = "Not now.";

        private Llm.Llm llm;

        private void Awake()
        {
            llm = GetComponent<Llm.Llm>();
        }

        protected override void RequestAnswer(ulong clientId, string message, Action<string> onAnswer)
        {
            if (llm == null)
            {
                Log.Warn($"Entity {name} has no llm to answer with");
                onAnswer(Fallback);
                return;
            }

            llm.Listen((long)clientId, message, onAnswer);
        }
    }
}
