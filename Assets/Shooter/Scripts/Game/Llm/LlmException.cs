using System;

namespace Shooter.Game
{
    public sealed class LlmException : Exception
    {
        public LlmException(string message) : base(message)
        {
        }
    }
}
