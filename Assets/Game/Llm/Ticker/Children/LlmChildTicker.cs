using UnityEngine;

namespace Shooter.Game.Llm.Ticker.Children
{
    public abstract class LlmChildTicker : MonoBehaviour
    {
        public abstract bool TickRequired(LlmStatus llmStatus);
        public abstract void RegisterTick();
    }
}
