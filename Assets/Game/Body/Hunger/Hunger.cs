using Unity.Netcode;
using UnityEngine;
using Shooter.Game.World;
using Shooter.Game.Core;

namespace Shooter.Game.Body
{
    public abstract class Hunger : NetworkBehaviour, IDigestible, IRestraint
    {
        public abstract float Amount { get; }
        public abstract float MaxAmount { get; }

        public abstract bool CanSpend(float a);
        public abstract void Spend(float a);
        public abstract void Restore(float a);

        public DigestionPriority Priority => DigestionPriority.Medium;
        public string Digest(DigestionDetail detail)
        {
            return detail == DigestionDetail.Full ? $"Hunger: {Amount} / {MaxAmount}" : null;
        }

        [SerializeField] private float sprintCost = 0.5f;
        [SerializeField] private float walkCost = 0.25f;
        [SerializeField] private float jumpCost = 2f;
        [SerializeField] private float idleCost = 0.1f;

        private void Awake()
        {
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsServer;
        }

        public override void OnNetworkDespawn()
        {
            enabled = false;
        }

        private void Update()
        {
            Spend(idleCost * Time.deltaTime * Clock.Current.Scale);
        }

        public bool CanPerform(ActionType type, float dt)
        {
            return !Blocker(type) || CanSpend(Cost(type, dt));
        }

        public void RegisterAction(ActionType type, float dt)
        {
            Spend(Cost(type, dt));
        }

        private float Cost(ActionType type, float dt)
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

        private bool Blocker(ActionType type)
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
