using UnityEngine;

namespace Shooter.Game.Llm
{
    public abstract class LlmChildTicker : MonoBehaviour
    {
        public abstract void RegisterTick();
        public abstract bool TickRequired(LlmStatus llmStatus);
    }
}
