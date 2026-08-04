using System;

namespace Shooter.Game.Speech
{
    public sealed class AITalker : Talker
    {
        private Llm.Llm llm;

        private void Awake()
        {
            llm = GetComponent<Llm.Llm>();
        }

        protected override void RequestAnswer(ulong clientId, string message, Action<string> onAnswer)
        {
            if (llm == null)
            {
                onAnswer($"Entity {name} has no llm to answer with");
                return;
            }

            llm.Listen((long)clientId, message, onAnswer);
        }
    }
}
