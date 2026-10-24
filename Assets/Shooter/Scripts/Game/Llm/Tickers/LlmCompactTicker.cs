using System;

namespace Shooter.Game
{
    [Serializable]
    public class LlmCompactTicker : LlmChildTicker
    {
        public override void Start()
        {
        }

        public override void RegisterTick()
        {
        }

        public override bool IsTickRequired(LlmStatus status)
        {
            return status.IsOverflowing;
        }
    }
}
