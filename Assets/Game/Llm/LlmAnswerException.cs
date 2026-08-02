namespace Shooter.Game.Llm
{
    public sealed class LlmAnswerException : LlmException
    {
        public LlmAnswerException(string message) : base(message)
        {
        }

        public override bool WorthRetrying => false;
    }
}
