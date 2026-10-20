using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Llm))]
    public sealed class AITalker : Talker
    {
        private Llm llm;

        protected override void ShooterAwake()
        {
            base.ShooterAwake();
            llm = GetComponent<Llm>();
        }

        protected override bool IsBusy()
        {
            return llm.IsThinking;
        }
    }
}
