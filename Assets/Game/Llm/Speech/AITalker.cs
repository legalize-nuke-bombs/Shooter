using Shooter.Game.Speech;
using UnityEngine;

namespace Shooter.Game.Llm
{
    // A resident one can talk with thinks while its mind is busy; what it hears comes through LlmConversationObserver
    [RequireComponent(typeof(Llm))]
    public sealed class AITalker : Talker
    {
        private Llm llm;

        protected override void Awake()
        {
            base.Awake();
            llm = GetComponent<Llm>();
        }

        protected override bool Busy()
        {
            return llm.Busy;
        }
    }
}
