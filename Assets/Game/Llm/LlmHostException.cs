namespace Shooter.Game.Llm
{
    public sealed class LlmHostException : LlmException
    {
        public LlmHostException(string message) : base(message)
        {
        }

        public override bool WorthRetrying => true;
    }
}
