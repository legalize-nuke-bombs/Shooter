using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(Health))]
    public class Falling : NetworkBehaviour
    {
        [SerializeField] private float safeHeight = 3f;

        [SerializeField] private float damagePerMetre = 12f;

        private Movement movement;
        private Health health;

        private void Awake()
        {
            movement = GetComponent<Movement>();
            health = GetComponent<Health>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            movement.Landed += Landed;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            movement.Landed -= Landed;
        }

        private void Landed(float height)
        {
            int damage = Mathf.RoundToInt((height - safeHeight) * damagePerMetre);
            if (damage <= 0) return;

            health.Damage(damage, null);
        }
    }
}
