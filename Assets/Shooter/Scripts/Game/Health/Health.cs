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

        public abstract bool Alive { get; }

        protected virtual void Awake()
        {
            earSpeaker = GetComponent<EarSpeaker>();
        }

        public string Digest(DigestionDetail detail)
        {
            return Alive ? $"Health: {Math.Round(Hp)}/{Math.Round(MaxHp)}" : "Dead";
        }

        public DigestionPriority Priority => DigestionPriority.High;

        public bool CanPerform(ActionType type, float dt)
        {
            return Alive;
        }

        public void RegisterAction(ActionType type, float dt)
        {
        }

        public event Action<double, Character, DamageAsset> Damaged;

        public DamageResult Damage(double amount, Character attacker, DamageAsset type)
        {
            if (!IsServer || !Alive || amount <= 0)
                return new DamageResult
                {
                    Murder = false
                };

            Log.Info($"Entity {name} got damage {amount} {type}");

            DamageRaw(amount);

            if (!Alive) Die();
            else earSpeaker.PlayForOwner(hurtSound);

            var result = new DamageResult
            {
                Murder = !Alive
            };

            if (Alive) Damaged?.Invoke(amount, attacker, type);

            return result;
        }

        protected abstract void DamageRaw(double amount);

        public abstract void Heal(double amount);

        public abstract void Resurrect();

        private void Die()
        {
            Log.Info($"Entity {name} died");

            foreach (IMortal mortal in GetComponents<IMortal>())
                mortal.Died();
        }
    }
}
