using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Server.Worlds.Items.Firearm
{
    public sealed class Ak47 : Firearm
    {
        public Ak47(long id, int magazine) : base(id, magazine)
        {
        }

        public override FirearmType FirearmType => Items.Firearm.FirearmType.Ak47;

        public override int MagazineSize => 30;

        public override StackableItem AmmoType => StackableItem.Ammo762X39;

        public override float Distance => 100f;

        public override int Damage => 25;

        public override float FireInterval => 0.1f;

        public override float ReloadTime => 2.5f;

        public override SoundType ShotSound => SoundType.Ak47Shot;

        public override SoundType MisfireSound => SoundType.Ak47Misfire;

        public override SoundType ReloadSound => SoundType.Ak47Reload;
    }
}
