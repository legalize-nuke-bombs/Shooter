using System;

namespace Shooter.Game
{
    [Serializable]
    public abstract class LlmChildTicker
    {
        public abstract void Start();
        public abstract void RegisterTick(float awakeSeconds);
        public abstract bool IsTickRequired(LlmStatus status, float awakeSeconds);
    }
}
