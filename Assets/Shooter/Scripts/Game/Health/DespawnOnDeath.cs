using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Health))]
    public class DespawnOnDeath : ShooterNetworkBehaviour
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
