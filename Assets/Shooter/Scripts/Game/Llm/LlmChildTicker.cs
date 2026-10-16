using System;

namespace Shooter.Game
{
    [Serializable]
    public abstract class LlmChildTicker
    {
        public abstract void OnStart();
        public abstract void RegisterTick();
        public abstract bool TickRequired(LlmStatus llmStatus);
    }
}
