using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RpcMovement))]
    [RequireComponent(typeof(Health))]
    public class FallDamageOnLanding : ShooterComponent
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

        protected override void ShooterOnEnable()
        {
            movement.Landed += DealFallDamage;
        }

        protected override void ShooterOnDisable()
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
