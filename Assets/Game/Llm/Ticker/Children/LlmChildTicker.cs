using UnityEngine;

namespace Shooter.Game.Llm.Ticker.Children
{
    public abstract class LlmChildTicker : MonoBehaviour
    {
        public abstract void RegisterTick();
        public abstract bool TickRequired(LlmStatus llmStatus);
    }
}
