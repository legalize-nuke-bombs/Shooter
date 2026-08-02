using System;

namespace Shooter.Game.Llm
{
    public abstract class LlmException : Exception
    {
        protected LlmException(string message) : base(message)
        {
        }

        public abstract bool WorthRetrying { get; }
    }
}
