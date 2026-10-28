using System;
using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public abstract class Health : NetworkBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        public abstract double Hp { get; }

        public abstract double MaxHp { get; }

        public abstract bool IsAlive { get; }

        public event Action<double, Character, DamageAsset> Damaged;

        public event Action Died;

        public event Action DeathFinished;

        public event Action<bool> AliveChanged;

        public string Digest(DigestionDetail detail)
        {
            return IsAlive ? $"Health: {Math.Round(Hp)}/{Math.Round(MaxHp)}" : "Dead";
        }

        public DigestionPriority Priority => DigestionPriority.High;

        public DamageResult TakeDamage(double amount, Character attacker, DamageAsset damageType)
        {
            if (!IsServer || !IsAlive || amount <= 0)
            {
                return new DamageResult
                {
                    IsLethal = false
                };
            }

            Log.Info($"Entity {name} got {amount} damage of {(damageType == null ? "no type" : damageType.name)}{(attacker == null ? "" : $" from {attacker.name}")}");

            ReduceHp(amount);

            if (IsAlive)
            {
                Damaged?.Invoke(amount, attacker, damageType);
            }
            else
            {
                AnnounceDeath();
            }

            return new DamageResult
            {
                IsLethal = !IsAlive
            };
        }

        protected abstract void ReduceHp(double amount);

        public abstract void Heal(double amount);

        public abstract void ResurrectWithFullHp();

        protected void AnnounceAliveChanged(bool alive)
        {
            AliveChanged?.Invoke(alive);
        }

        private void AnnounceDeath()
        {
            Log.Info($"Entity {name} died");
            Died?.Invoke();
            DeathFinished?.Invoke();
        }
    }
}
