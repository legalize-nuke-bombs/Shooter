using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class DefaultHunger : Hunger, ISaveableComponent
    {
        [SerializeField] private float maxAmount = 100f;

        private readonly NetworkVariable<float> amount = new(0f, NetworkVariableReadPermission.Owner);

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

        public override float Amount => amount.Value;

        public override float MaxAmount => Mathf.Max(maxAmount, 1f);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer) return;

            amount.SetUpdateTraits(new NetworkVariableUpdateTraits { MinSecondsBetweenUpdates = 0.1f });
            amount.Value = MaxAmount;
        }

        public override bool CanSpendAmount(float cost)
        {
            return amount.Value >= cost;
        }

        public override void SpendAmount(float cost)
        {
            amount.Value = Mathf.Max(amount.Value - Mathf.Max(cost, 0f), 0f);
        }

        public override void RestoreAmount(float restoredAmount)
        {
            amount.Value = Mathf.Min(amount.Value + Mathf.Max(restoredAmount, 0f), MaxAmount);
        }
    }
}
