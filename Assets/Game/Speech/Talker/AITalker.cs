using Shooter.Logging;

namespace Shooter.Game.Speech
{
    public sealed class AITalker : Talker
    {
        private static readonly Journal Log = Logs.Here();

        private Llm.Llm llm;

        protected override void Awake()
        {
            base.Awake();
            llm = GetComponent<Llm.Llm>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer || llm == null) return;
            llm.Answered += DeliverAnswer;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && llm != null) llm.Answered -= DeliverAnswer;
            base.OnNetworkDespawn();
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
                DeliverAnswer(wandererId, new Answer()
                {
                    Content = "Not now.",
                    Loud = false
                });
                return;
            }

            llm.Notice($"Wanderer [ID {wandererId}] says: {message}", true, wandererId);
        }
    }
}
