using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(RpcMovement))]
    [RequireComponent(typeof(Health))]
    public class FallDamageOnLanding : ShooterBehaviour
    {
        [SerializeField] private float safeHeight = 3f;
        [SerializeField] private float damagePerMetre = 12f;
        [SerializeField] private DamageAsset fallDamage;

        private RpcMovement movement;
        private Health health;

        protected override void ShooterAwake()
        {
            movement = GetComponent<RpcMovement>();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            movement.Landed += DealFallDamage;
        }

        private void OnDisable()
        {
            movement.Landed -= DealFallDamage;
        }

        private void DealFallDamage(float fallHeight)
        {
            int damage = Mathf.RoundToInt((fallHeight - safeHeight) * damagePerMetre);
            if (damage > 0) health.TakeDamage(damage, null, fallDamage);
        }
    }
}
