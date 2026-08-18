using System;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public abstract class Health : NetworkBehaviour, IDigestible, IRestraint
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private EarSoundSpec hurtSound;

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
            return Alive ? $"Health: {Hp}/{MaxHp}" : "Dead";
        }

        public DigestionPriority Priority => DigestionPriority.High;

        public bool CanPerform(ActionType type, float dt)
        {
            return Alive;
        }

        public void RegisterAction(ActionType type, float dt)
        {
        }

        public event Action<double, long?, DamageSpec> Damaged;

        public DamageResult Damage(double amount, long? attackerId, DamageSpec type)
        {
            if (!IsServer || !Alive || amount <= 0)
                return new DamageResult
                {
                    Murder = false
                };

            Log.Info($"Entity {this.NameOf()} got damage {amount} {type}");

            DamageRaw(amount);

            if (!Alive) Die();
            else earSpeaker.Play(hurtSound);

            var result = new DamageResult
            {
                Murder = !Alive
            };

            if (Alive) Damaged?.Invoke(amount, attackerId, type);

            return result;
        }

        protected abstract void DamageRaw(double amount);

        public abstract void Heal(double amount);

        public abstract void Resurrect();

        private void Die()
        {
            Log.Info($"Entity {this.NameOf()} died");

            foreach (IMortal mortal in this.FindAll<IMortal>())
                mortal.Died();
        }
    }
}
