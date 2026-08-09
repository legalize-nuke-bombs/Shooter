using System;

namespace Shooter.Game.Speech
{
    public sealed class RefusiveTalker : Talker
    {
        private const string Refusal = "Not now.";

        protected override void RequestAnswer(long wandererId, string message, Action<string> onAnswer)
        {
            onAnswer(Refusal);
        }
    }
}
