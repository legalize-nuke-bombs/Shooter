using Shooter.Game.Core;
using Shooter.Game.World;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public abstract class Hunger : NetworkBehaviour, IDigestible, IRestraint
    {
        [SerializeField] private float sprintCost = 0.5f;
        [SerializeField] private float walkCost = 0.25f;
        [SerializeField] private float jumpCost = 2f;
        [SerializeField] private float idleCost = 0.1f;
        public abstract float Amount { get; }
        public abstract float MaxAmount { get; }

        private void Awake()
        {
            enabled = false;
        }

        private void Update()
        {
            Spend(idleCost * Time.deltaTime * Clock.Current.Scale);
        }

        public DigestionPriority Priority => DigestionPriority.Medium;

        public string Digest(DigestionDetail detail)
        {
            return detail == DigestionDetail.Full
                ? $"Hunger: {Mathf.RoundToInt(Amount)} / {Mathf.RoundToInt(MaxAmount)}"
                : null;
        }

        public bool CanPerform(ActionType type, float dt)
        {
            return !Blocker(type) || CanSpend(Cost(type, dt));
        }

        public void RegisterAction(ActionType type, float dt)
        {
            Spend(Cost(type, dt));
        }

        public abstract bool CanSpend(float a);
        public abstract void Spend(float a);
        public abstract void Restore(float a);

        public override void OnNetworkSpawn()
        {
            enabled = IsServer;
        }

        public override void OnNetworkDespawn()
        {
            enabled = false;
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
