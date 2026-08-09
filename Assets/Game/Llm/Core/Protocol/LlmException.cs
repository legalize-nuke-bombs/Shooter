using System;

namespace Shooter.Game.Llm
{
    public sealed class LlmException : Exception
    {
        public LlmException(string message) : base(message)
        {
        }
    }
}
