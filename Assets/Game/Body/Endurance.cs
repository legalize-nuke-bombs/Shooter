using System;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Llm;

namespace Shooter.Game.Body
{
    public class Endurance : NetworkBehaviour, IDigestible, IRestraint
    {
        [SerializeField] private float maxAmount = 100f;
        [SerializeField] private float sprintCost = 25f;
        [SerializeField] private float walkCost = 3f;
        [SerializeField] private float jumpCost = 10f;
        [SerializeField] private float recoverySpeed = 12f;
        [SerializeField] private float sprintThreshold = 10f;

        private readonly NetworkVariable<float> amount = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Owner);

        private bool exhausted;

        private void Awake()
        {
            enabled = false;
        }

        public float Amount => amount.Value;

        public float MaxAmount => Mathf.Max(maxAmount, 1f);

        public DigestionPriority Priority => DigestionPriority.Low;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            enabled = true;
            amount.SetUpdateTraits(new NetworkVariableUpdateTraits { MinSecondsBetweenUpdates = 0.1f });
            amount.Value = MaxAmount;
        }

        public override void OnNetworkDespawn()
        {
            enabled = false;
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail != DigestionDetail.Full) return null;

            return $"Stamina: {Mathf.RoundToInt(Amount)}/{Mathf.RoundToInt(MaxAmount)}";
        }

        private void Update()
        {
            amount.Value = Mathf.Min(amount.Value + Time.deltaTime * recoverySpeed, MaxAmount);

            if (amount.Value >= sprintThreshold) exhausted = false;
        }

        public bool CanPerform(ActionType type, float dt)
        {
            if (type == ActionType.Sprint && exhausted) return false;

            return !Blocker(type) || (amount.Value >= Cost(type, dt));
        }

        public void RegisterAction(ActionType type, float dt)
        {
            amount.Value = Math.Max(0, amount.Value - Cost(type, dt));

            if (amount.Value <= 0f) exhausted = true;
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
