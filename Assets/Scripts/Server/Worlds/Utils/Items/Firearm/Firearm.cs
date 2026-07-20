using Shooter.Server.Worlds.Utils.CharSpecs.InventoryKeeper;

namespace Shooter.Server.Worlds.Utils.Items.Firearm
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

        public bool TryToShot()
        {
            if (magazine == 0)
            {
                return false;
            }

            magazine--;

            return true;
        }

        protected abstract FirearmType FirearmType();
        protected abstract int MagazineSize();
        protected abstract StackableItem AmmoType();

        public override UniqueItemState State()
        {
            return new FirearmState
            {
                Id = Id,
                Magazine = magazine,
                FirearmType = FirearmType(),
                MagazineSize = MagazineSize(),
                AmmoType = AmmoType()
            };
        }
    }
}
