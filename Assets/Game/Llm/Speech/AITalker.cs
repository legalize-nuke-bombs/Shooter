using Shooter.Game.Speech;
using Shooter.Logging;

namespace Shooter.Game.Llm
{
    public sealed class AITalker : Talker
    {
        private static readonly Journal Log = Logs.Here();

        private Llm llm;

        protected override void Awake()
        {
            base.Awake();
            llm = GetComponent<Llm>();
        }

        protected override bool Busy()
        {
            return llm != null && llm.Busy;
        }

        protected override void RequestAnswer(long wandererId, string message)
        {
            if (llm == null)
            {
                Log.Warn($"Entity {name} has no llm to answer with");
                Refuse(wandererId);
                return;
            }

            llm.Notice($"Wanderer [ID {wandererId}] says: {message}", true, wandererId);
        }
    }
}
