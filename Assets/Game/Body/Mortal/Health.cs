using Shooter.Game.Relationship;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Llm;

namespace Shooter.Game.Body
{
    public abstract class Health : NetworkBehaviour, IDigestible, IRestraint
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private EarSoundSpec hurtSound;

        private CharacterRelation characterRelation;
        private EarSpeaker earSpeaker;

        protected virtual void Awake()
        {
            characterRelation = GetComponent<CharacterRelation>();
            earSpeaker = GetComponent<EarSpeaker>();
        }

        public bool CanPerform(ActionType type, float dt)
        {
            return Alive;
        }
        public void RegisterAction(ActionType type, float dt) { }

        public abstract int Hp { get; }

        public abstract int MaxHp { get; }

        public abstract bool Alive { get; }

        public DamageResult Damage(int amount, long? attackerId)
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
            else earSpeaker.Play(hurtSound);

            var result = new DamageResult()
            {
                Murder = !Alive
            };

            if (Alive && characterRelation != null && attackerId != null)
            {
                characterRelation.DecreaseAmount(attackerId.Value, amount, $"{attackerId.Value} dealt {amount} damage");
            }

            return result;
        }

        protected abstract void DamageRaw(int amount);

        public abstract void Heal(int amount);

        public abstract void Resurrect();

        public string Digest(DigestionDetail detail)
        {
            return Alive ? $"Health: {Hp}/{MaxHp}" : "Dead";
        }

        public DigestionPriority Priority => DigestionPriority.Medium;

        protected void Die()
        {
            Log.Info($"Entity {name} died");

            foreach (IMortal mortal in GetComponents<IMortal>())
                mortal.Died();
        }
    }
}
