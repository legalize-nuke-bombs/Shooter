using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Hands))]
    public abstract class Hunger : ShooterNetworkBehaviour, IDigestible
    {
        [SerializeField] private float sprintCost = 0.5f;
        [SerializeField] private float walkCost = 0.25f;
        [SerializeField] private float jumpCost = 2f;
        [SerializeField] private float idleCost = 0.1f;

        private Clock clock;
        private Hands hands;

        public abstract float Amount { get; }
        public abstract float MaxAmount { get; }

        protected override void ShooterAwake()
        {
            clock = Registers.Get<Clock>();
            hands = GetComponent<Hands>();
            enabled = false;
        }

        private void OnEnable()
        {
            hands.Checking += CanAfford;
            hands.Performed += SpendOn;
        }

        private void OnDisable()
        {
            hands.Checking -= CanAfford;
            hands.Performed -= SpendOn;
        }

        private void Update()
        {
            SpendAmount(idleCost * Time.deltaTime * clock.Scale);
        }

        public DigestionPriority Priority => DigestionPriority.Medium;

        public string Digest(DigestionDetail detail)
        {
            return detail == DigestionDetail.Full ? $"Hunger: {Mathf.RoundToInt(Amount)} / {Mathf.RoundToInt(MaxAmount)}" : null;
        }

        public abstract bool CanSpendAmount(float cost);
        public abstract void SpendAmount(float cost);
        public abstract void RestoreAmount(float restoredAmount);

        public override void OnNetworkSpawn()
        {
            enabled = IsServer;
        }

        public override void OnNetworkDespawn()
        {
            enabled = false;
        }

        private bool CanAfford(ActionType type, float duration)
        {
            return !IsLimitedByHunger(type) || CanSpendAmount(GetActionCost(type, duration));
        }

        private void SpendOn(ActionType type, float duration)
        {
            SpendAmount(GetActionCost(type, duration));
        }

        private float GetActionCost(ActionType type, float duration)
        {
            switch (type)
            {
                case ActionType.Sprint:
                    return sprintCost * duration;
                case ActionType.Walk:
                    return walkCost * duration;
                case ActionType.Jump:
                    return jumpCost;
                default:
                    return 0;
            }
        }

        private bool IsLimitedByHunger(ActionType type)
        {
            switch (type)
            {
                case ActionType.Sprint:
                case ActionType.Jump:
                    return true;
                default:
                    return false;
            }
        }
    }
}
