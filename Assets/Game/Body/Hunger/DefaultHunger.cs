using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class DefaultHunger : Hunger, ISaveableComponent
    {
        [SerializeField] private float maxAmount = 100f;

        private readonly NetworkVariable<float> amount = new(0f, NetworkVariableReadPermission.Owner);

        public string ComponentKey => "Hunger";
        struct SaveData
        {
            public float Amount { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Amount = amount.Value
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            amount.Value = sd.Amount;
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

        public override bool CanSpend(float a)
        {
            return amount.Value >= a;
        }

        public override void Spend(float a)
        {
            amount.Value = Mathf.Max(amount.Value - Mathf.Max(a, 0f), 0f);
        }

        public override void Restore(float a)
        {
            amount.Value = Mathf.Min(amount.Value + Mathf.Max(a, 0f), MaxAmount);
        }
    }
}
