using System;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Game.Body
{
    public abstract class Health : NetworkBehaviour, IDigestible, IRestraint
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private EarSoundSpec hurtSound;

        private EarSpeaker earSpeaker;

        public event Action<double, long?, DamageSpec> Damaged;

        protected virtual void Awake()
        {
            earSpeaker = GetComponent<EarSpeaker>();
        }

        public bool CanPerform(ActionType type, float dt)
        {
            return Alive;
        }
        public void RegisterAction(ActionType type, float dt) { }

        public abstract double Hp { get; }

        public abstract double MaxHp { get; }

        public abstract bool Alive { get; }

        public DamageResult Damage(double amount, long? attackerId, DamageSpec type)
        {
            if (!IsServer || !Alive || amount <= 0)
            {
                return new DamageResult()
                {
                    Murder = false
                };
            }

            DamageRaw(amount);

            if (!Alive) Die();
            else if (type.Loud) earSpeaker.Play(hurtSound);

            var result = new DamageResult()
            {
                Murder = !Alive
            };

            if (Alive) Damaged?.Invoke(amount, attackerId, type);

            return result;
        }

        protected abstract void DamageRaw(double amount);

        public abstract void Heal(double amount);

        public abstract void Resurrect();

        public string Digest(DigestionDetail detail)
        {
            return Alive ? $"Health: {Hp}/{MaxHp}" : "Dead";
        }

        public DigestionPriority Priority => DigestionPriority.High;

        private void Die()
        {
            Log.Info($"Entity {name} died");

            foreach (IMortal mortal in this.FindAll<IMortal>())
                mortal.Died();
        }
    }
}
