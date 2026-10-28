using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class DespawnOnDeath : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.DeathFinished += Despawn;
        }

        private void OnDisable()
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
