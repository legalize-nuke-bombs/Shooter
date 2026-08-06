using Shooter.Game.Relationship;
using Shooter.Logging;
using Unity.Netcode;

namespace Shooter.Game.Body
{
    public abstract class Health : NetworkBehaviour, IDigestible, IRestraint
    {
        private static readonly Journal Log = Logs.Here();

        private CharacterRelation characterRelation;

        protected virtual void Awake()
        {
            characterRelation = GetComponent<CharacterRelation>();
        }

        public bool Restrains => !Alive;

        public abstract int Hp { get; }

        public abstract int MaxHp { get; }

        public abstract bool Alive { get; }

        public DamageResult Damage(int amount, string attackerName)
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
            var result = new DamageResult()
            {
                Murder = !Alive
            };

            if (characterRelation != null && attackerName != null)
            {
                characterRelation.DecreaseAmount(attackerName, amount, $"{attackerName} dealt {amount} damage");
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
            Log.Info("Entity {} died", name);

            foreach (IMortal mortal in GetComponents<IMortal>())
                mortal.Died();
        }
    }
}
