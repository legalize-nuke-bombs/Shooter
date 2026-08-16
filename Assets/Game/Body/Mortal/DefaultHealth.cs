using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public sealed class DefaultHealth : Health
    {
        [SerializeField] private double maxHp = 100;

        private readonly NetworkVariable<double> hp = new NetworkVariable<double>();

        public override double Hp => hp.Value;

        public override double MaxHp => System.Math.Max(maxHp, 1.0d);

        public override bool Alive => hp.Value > 0;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            hp.Value = MaxHp;
        }

        protected override void DamageRaw(double amount)
        {
            hp.Value = System.Math.Max(hp.Value - amount, 0);
        }

        public override void Heal(double amount)
        {
            if (!IsServer || !Alive || amount <= 0) return;

            hp.Value = System.Math.Min(hp.Value + amount, MaxHp);
        }

        public override void Resurrect()
        {
            if (!IsServer || Alive) return;

            hp.Value = MaxHp;
        }
    }
}
