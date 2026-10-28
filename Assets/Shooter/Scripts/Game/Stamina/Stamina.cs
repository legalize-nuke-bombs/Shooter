using System;
using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Hands))]
    public class Stamina : ShooterNetworkComponent, IDigestible, ISaveableComponent, ITimePassedHandler
    {
        [SerializeField] private float maxAmount = 100f;
        [SerializeField] private float sprintCost = 25f;
        [SerializeField] private float walkCost = 3f;
        [SerializeField] private float jumpCost = 10f;
        [SerializeField] private float recoveryPerHour = 1200f;
        [SerializeField] private float sprintThreshold = 10f;

        private readonly NetworkVariable<float> amount = new(0f, NetworkVariableReadPermission.Owner);

        private Hands hands;
        private bool exhausted;

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

        protected override void ShooterAwake()
        {
            hands = GetComponent<Hands>();
            enabled = false;
        }

        protected override void ShooterOnEnable()
        {
            hands.Checking += CanAfford;
            hands.Performed += SpendOn;
        }

        protected override void ShooterOnDisable()
        {
            hands.Checking -= CanAfford;
            hands.Performed -= SpendOn;
        }

        public void HandleTimePassed(float seconds)
        {
            amount.Value = Mathf.Min(amount.Value + recoveryPerHour * seconds / Clock.HourLengthSeconds, MaxAmount);

            if (amount.Value >= sprintThreshold) exhausted = false;
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        public string Digest(DigestionDetail detail)
        {
            if (detail != DigestionDetail.Full) return null;

            return $"Stamina: {Mathf.RoundToInt(Amount)}/{Mathf.RoundToInt(MaxAmount)}";
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

        private bool CanAfford(ActionType type, float duration)
        {
            if (type == ActionType.Sprint && exhausted) return false;

            return !IsLimitedByStamina(type) || amount.Value >= GetActionCost(type, duration);
        }

        private void SpendOn(ActionType type, float duration)
        {
            amount.Value = Math.Max(0, amount.Value - GetActionCost(type, duration));

            if (amount.Value <= 0f) exhausted = true;
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
