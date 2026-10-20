using System;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public abstract class Health : ShooterNetworkBehaviour, IDigestible, IRestraint
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

        public bool CanPerform(ActionType type, float dt)
        {
            return IsAlive;
        }

        public void RegisterAction(ActionType type, float dt)
        {
        }

        public DamageResult TakeDamage(double amount, Character attacker, DamageAsset damageType)
        {
            if (!IsServer || !IsAlive || amount <= 0)
            {
                return new DamageResult
                {
                    IsLethal = false
                };
            }

            Log.Info($"Entity {name} got damage {amount} {damageType}");

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

        public abstract void RestoreHp(double amount);

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
