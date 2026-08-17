using Shooter.Game.Body.Bleeding;
using Shooter.Game.AI;
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

        private AICharacterRelation aiCharacterRelation;
        private Bleeder bleeder;
        private EarSpeaker earSpeaker;

        protected virtual void Awake()
        {
            aiCharacterRelation = GetComponent<AICharacterRelation>();
            bleeder = GetComponent<Bleeder>();
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

        public DamageResult Damage(double amount, long? attackerId, bool silent = false)
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
            else if (!silent) earSpeaker.Play(hurtSound);

            var result = new DamageResult()
            {
                Murder = !Alive
            };

            if (Alive && !silent)
            {
                if (aiCharacterRelation != null && attackerId != null)
                {
                    aiCharacterRelation.OnDamage(attackerId.Value, amount);
                }

                if (bleeder != null)
                {
                    bleeder.Bleed(2 * amount);
                }
            }

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

            foreach (IMortal mortal in GetComponents<IMortal>())
                mortal.Died();
        }
    }
}
