using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public abstract class Health : NetworkBehaviour, IDigestible, IRestraint
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private EarSoundAsset hurtSound;

        private EarSpeaker earSpeaker;

        public abstract double Hp { get; }

        public abstract double MaxHp { get; }

        public abstract bool IsAlive { get; }

        protected virtual void Awake()
        {
            earSpeaker = GetComponent<EarSpeaker>();
        }

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

        public event Action<double, Character, DamageAsset> Damaged;

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

            if (!IsAlive)
            {
                AnnounceDeath();
            }
            else
            {
                earSpeaker.PlayForOwner(hurtSound);
            }

            DamageResult result = new()
            {
                IsLethal = !IsAlive
            };

            if (IsAlive) Damaged?.Invoke(amount, attacker, damageType);

            return result;
        }

        protected abstract void ReduceHp(double amount);

        public abstract void RestoreHp(double amount);

        public abstract void ResurrectWithFullHp();

        private void AnnounceDeath()
        {
            Log.Info($"Entity {name} died");

            foreach (IMortal mortal in GetComponents<IMortal>())
            {
                mortal.HandleDeath();
            }
        }
    }
}
