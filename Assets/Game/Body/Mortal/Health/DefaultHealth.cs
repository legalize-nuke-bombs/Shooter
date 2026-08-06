using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public sealed class DefaultHealth : Health
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private int maxHp = 100;

        private readonly NetworkVariable<int> hp = new NetworkVariable<int>();

        public override int Hp => hp.Value;

        public override int MaxHp => Mathf.Max(maxHp, 1);

        public override bool Alive => hp.Value > 0;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            hp.Value = MaxHp;
        }

        protected override void DamageRaw(int amount)
        {
            hp.Value = Mathf.Max(hp.Value - amount, 0);
            Log.Info("Entity {} took {} damage, hp now {}/{}", name, amount, hp.Value, MaxHp);
        }

        public override void Heal(int amount)
        {
            if (!IsServer || !Alive || amount <= 0) return;

            hp.Value = Mathf.Min(hp.Value + amount, MaxHp);
            Log.Info("Entity {} healed {}, hp now {}/{}", name, amount, hp.Value, MaxHp);
        }

        public override void Resurrect()
        {
            if (!IsServer || Alive) return;

            hp.Value = MaxHp;
            Log.Info("Entity {} resurrected with {} hp", name, hp.Value);
        }
    }
}
