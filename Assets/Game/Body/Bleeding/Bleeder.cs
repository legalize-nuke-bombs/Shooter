using System;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Core;
using Shooter.Game.World;

namespace Shooter.Game.Body.Bleeding
{
    [RequireComponent(typeof(Health))]
    public class Bleeder : NetworkBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private Health health;

        [SerializeField] private DamageSpec bleedingDamage;

        private readonly NetworkVariable<double> level = new NetworkVariable<double>(0);

        public double Level => level.Value;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
        }

        private void OnDamaged(double amount, long? attackerId, DamageSpec type)
        {
            if (type.Bleed <= 0) return;

            Bleed(amount * type.Bleed);
        }

        private float timer = 0;
        [SerializeField] private float timerInterval = 0.1f;
        private void Update()
        {
            if (!IsServer) return;
            timer += Time.deltaTime * Clock.Current.Scale;
            if (timer >= timerInterval)
            {
                Tick(timer);
                timer = 0;
            }
        }

        [SerializeField] private float recoverySpeed = 1f;
        [SerializeField] private float damageCoefficient = 0.025f;
        private void Tick(float dt)
        {
            if (level.Value < 0.01f)
            {
                return;
            }

            level.Value = Math.Max(0, level.Value - dt * recoverySpeed);

            health.Damage(level.Value * damageCoefficient * dt, null, bleedingDamage);
        }

        public void Bleed(double force)
        {
            force = Math.Max(0, force);
            level.Value = Math.Min(100, level.Value + force);
        }

        public void Recover(double force)
        {
            force = Math.Max(0, force);
            level.Value = Math.Max(0, level.Value - force);
        }

        public string Digest(DigestionDetail detail)
        {
            if (detail == DigestionDetail.Brief)
            {
                return null;
            }

            return level.Value < 0.01f
                ? "No bleeding"
                : $"Bleeding {level.Value:F0} / 100";
        }

        public DigestionPriority Priority => DigestionPriority.High;
    }
}
