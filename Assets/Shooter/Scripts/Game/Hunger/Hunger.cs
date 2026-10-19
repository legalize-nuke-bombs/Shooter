using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public abstract class Hunger : NetworkBehaviour, IDigestible, IRestraint
    {
        [SerializeField] private float sprintCost = 0.5f;
        [SerializeField] private float walkCost = 0.25f;
        [SerializeField] private float jumpCost = 2f;
        [SerializeField] private float idleCost = 0.1f;

        private Clock clock;

        public abstract float Amount { get; }
        public abstract float MaxAmount { get; }

        private void Awake()
        {
            clock = Registers.GetAll<Clock>(RegisterInactivePolicy.Include).Single();
            enabled = false;
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

        public bool CanPerform(ActionType type, float dt)
        {
            return !IsLimitedByHunger(type) || CanSpendAmount(GetActionCost(type, dt));
        }

        public void RegisterAction(ActionType type, float dt)
        {
            SpendAmount(GetActionCost(type, dt));
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

        private float GetActionCost(ActionType type, float dt)
        {
            switch (type)
            {
                case ActionType.Sprint:
                    return sprintCost * dt;
                case ActionType.Walk:
                    return walkCost * dt;
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
