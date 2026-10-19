using System;
using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Stamina : NetworkBehaviour, IDigestible, IRestraint, ISaveableComponent
    {
        [SerializeField] private float maxAmount = 100f;
        [SerializeField] private float sprintCost = 25f;
        [SerializeField] private float walkCost = 3f;
        [SerializeField] private float jumpCost = 10f;
        [SerializeField] private float recoverySpeed = 12f;
        [SerializeField] private float sprintThreshold = 10f;

        private readonly NetworkVariable<float> amount = new(0f, NetworkVariableReadPermission.Owner);

        private Clock clock;
        private bool exhausted;

        public string ComponentKey => "Endurance";

        private struct SaveData
        {
            public float Amount { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Amount = amount.Value
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            amount.Value = saveData.Amount;
        }

        public float Amount => amount.Value;

        public float MaxAmount => Mathf.Max(maxAmount, 1f);

        private void Awake()
        {
            clock = Registers.GetAll<Clock>(RegisterInactivePolicy.Include).Single();
            enabled = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime * clock.Scale;
            amount.Value = Mathf.Min(amount.Value + dt * recoverySpeed, MaxAmount);

            if (amount.Value >= sprintThreshold) exhausted = false;
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        public string Digest(DigestionDetail detail)
        {
            if (detail != DigestionDetail.Full) return null;

            return $"Stamina: {Mathf.RoundToInt(Amount)}/{Mathf.RoundToInt(MaxAmount)}";
        }

        public bool CanPerform(ActionType type, float dt)
        {
            if (type == ActionType.Sprint && exhausted) return false;

            return !IsLimitedByStamina(type) || amount.Value >= GetActionCost(type, dt);
        }

        public void RegisterAction(ActionType type, float dt)
        {
            amount.Value = Math.Max(0, amount.Value - GetActionCost(type, dt));

            if (amount.Value <= 0f) exhausted = true;
        }

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

        private bool IsLimitedByStamina(ActionType type)
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
