using System;

namespace Shooter.Game
{
    [Serializable]
    public class LlmCompactTicker : LlmChildTicker
    {
        public override void OnStart()
        {
        }

        public override void RegisterTick()
        {
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            return llmStatus.PendingCompact;
        }
    }
}
