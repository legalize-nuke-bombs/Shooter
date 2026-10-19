using System;

namespace Shooter.Game
{
    [Serializable]
    public class LlmMailTicker : LlmChildTicker
    {
        public override void StartTicking()
        {
        }

        public override void RegisterTick()
        {
        }

        public override bool IsTickRequired(LlmStatus status)
        {
            return status.HasUnseenUrgentEvent;
        }
    }
}
