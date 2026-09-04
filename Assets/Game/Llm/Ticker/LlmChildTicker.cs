using System;

namespace Shooter.Game.Llm
{
    [Serializable]
    public abstract class LlmChildTicker
    {
        public abstract void RegisterTick();
        public abstract bool TickRequired(LlmStatus llmStatus);
    }
}
