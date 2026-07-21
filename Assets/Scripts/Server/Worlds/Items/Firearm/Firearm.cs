using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Server.Worlds.Items.Firearm
{
    public abstract class Firearm : UniqueItem
    {
        private int magazine;
        protected Firearm(long id, int magazine) : base(id)
        {
            this.magazine = magazine;
        }

        public bool CanShoot()
        {
            return (magazine > 0);
        }

        public bool TryToShoot()
        {
            if (magazine == 0)
            {
                return false;
            }

            magazine--;

            return true;
        }

        public abstract FirearmType FirearmType();
        public abstract int MagazineSize();
        public abstract StackableItem AmmoType();
        public abstract float Distance();
        public abstract int Damage();
        public abstract float FireInterval();
        public abstract SoundType ShotSound();
        public abstract SoundType MisfireSound();

        public override UniqueItemState State()
        {
            return new FirearmState
            {
                Id = Id,
                Magazine = magazine,
                FirearmType = FirearmType(),
                MagazineSize = MagazineSize(),
                AmmoType = AmmoType(),
                Distance = Distance(),
                Damage = Damage()
            };
        }
    }
}
