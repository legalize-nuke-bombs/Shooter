using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class Endurance : NetworkBehaviour, IDigestible
    {
        [SerializeField] private float maxAmount = 100f;
        [SerializeField] private float sprintCost = 25f;
        [SerializeField] private float walkCost = 3f;
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

        public bool Sprint(float dt)
        {
            if (exhausted && amount.Value < sprintThreshold) return false;

            float cost = sprintCost * dt;
            if (cost > amount.Value)
            {
                exhausted = true;
                return false;
            }

            exhausted = false;
            amount.Value -= cost;

            return true;
        }

        public void Walk(float dt)
        {
            amount.Value = Mathf.Max(amount.Value - walkCost * dt, 0f);
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail != DigestionDetail.Full) return null;

            return $"Stamina: {Mathf.RoundToInt(Amount)}/{Mathf.RoundToInt(MaxAmount)}";
        }

        private void Update()
        {
            amount.Value = Mathf.Min(amount.Value + Time.deltaTime * recoverySpeed, MaxAmount);
        }
    }
}
