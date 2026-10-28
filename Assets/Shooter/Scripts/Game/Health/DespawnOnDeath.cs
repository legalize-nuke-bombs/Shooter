using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class DespawnOnDeath : ShooterNetworkComponent
    {
        private static readonly Journal Log = Logs.Here();

        private Health health;

        protected override void ShooterAwake()
        {
            health = GetComponent<Health>();
        }

        protected override void ShooterOnEnable()
        {
            health.DeathFinished += Despawn;
        }

        protected override void ShooterOnDisable()
        {
            health.DeathFinished -= Despawn;
        }

        public override void OnNetworkDespawn()
        {
            gameObject.SetActive(false);
        }

        private void Despawn()
        {
            Log.Info($"Entity {name} leaves the world after death");
            NetworkObject.Despawn(!NetworkObject.InScenePlaced);
        }
    }
}
