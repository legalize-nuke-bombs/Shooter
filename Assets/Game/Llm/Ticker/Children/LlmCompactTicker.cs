namespace Shooter.Game.Llm.Ticker.Children
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
