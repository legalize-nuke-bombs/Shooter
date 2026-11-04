using System;

namespace Shooter.Game
{
    [Serializable]
    public class LlmCompactTicker : LlmChildTicker
    {
        public override void Start()
        {
        }

        public override void RegisterTick(float awakeSeconds)
        {
        }

        public override bool IsTickRequired(LlmStatus status, float awakeSeconds)
        {
            return status.IsOverflowing;
        }
    }
}
