using System;

namespace Shooter.Game
{
    [Serializable]
    public class LlmMailTicker : LlmChildTicker
    {
        public override void OnStart()
        {
        }

        public override void RegisterTick()
        {
        }

        public override bool TickRequired(LlmStatus llmStatus)
        {
            return llmStatus.PendingMail;
        }
    }
}
