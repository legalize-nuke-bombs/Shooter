using Shooter.Logging;
using Unity.Netcode;

namespace Shooter.Game.Body
{
    public abstract class Health : NetworkBehaviour, IDigestible, IRestraint
    {
        private static readonly Journal Log = Logs.Here();

        public bool Restrains => !Alive;

        public abstract int Hp { get; }

        public abstract int MaxHp { get; }

        public abstract bool Alive { get; }

        public abstract void Damage(int amount);

        public abstract void Heal(int amount);

        public abstract void Resurrect();

        public string Digest()
        {
            return Alive ? $"Здоровье: {Hp}/{MaxHp}" : "Мертв";
        }

        protected void Die()
        {
            Log.Info("Entity {} died", name);

            foreach (IMortal mortal in GetComponents<IMortal>())
                mortal.Died();
        }
    }
}
