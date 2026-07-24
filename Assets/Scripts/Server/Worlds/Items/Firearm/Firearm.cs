using System;
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

        public abstract FirearmType FirearmType { get; }
        public abstract int MagazineSize { get; }
        public abstract StackableItem AmmoType { get; }
        public abstract float Distance { get; }
        public abstract int Damage { get; }
        public abstract float FireInterval { get; }
        public abstract float ReloadTime { get; }
        public abstract SoundType ShotSound { get; }
        public abstract SoundType MisfireSound { get; }
        public abstract SoundType ReloadSound { get; }

        public bool CanShoot => magazine > 0;

        public bool MagazineFull => magazine == MagazineSize;

        public bool TryShoot()
        {
            if (magazine == 0) return false;

            magazine--;
            return true;
        }

        public int Reload(int toAddRequested)
        {
            int absent = MagazineSize - magazine;
            int toAdd = Math.Min(absent, toAddRequested);
            magazine += toAdd;
            return toAdd;
        }

        public override UniqueItemState State()
        {
            return new FirearmState
            {
                Id = Id,
                Magazine = magazine,
                MagazineSize = MagazineSize,
                FirearmType = FirearmType
            };
        }
    }
}
