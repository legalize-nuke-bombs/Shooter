namespace Shooter.Game.Llm
{
    public class LlmCompactTicker : LlmChildTicker
    {
        public override void RegisterTick()
        {
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            return llmStatus.PendingCompact;
        }
    }
}
