using Shooter.Game.Speech;
using UnityEngine;

namespace Shooter.Game.Llm
{
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
