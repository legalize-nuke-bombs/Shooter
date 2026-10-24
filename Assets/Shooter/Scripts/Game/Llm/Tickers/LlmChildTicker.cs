using System;

namespace Shooter.Game
{
    [Serializable]
    public abstract class LlmChildTicker
    {
        public abstract void Start();
        public abstract void RegisterTick();
        public abstract bool IsTickRequired(LlmStatus status);
    }
}
