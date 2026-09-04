using System;

namespace Shooter.Game.Llm
{
    [Serializable]
    public class LlmTalkTicker : LlmChildTicker
    {
        public override void RegisterTick()
        {
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            return llmStatus.PendingTable;
        }
    }
}
