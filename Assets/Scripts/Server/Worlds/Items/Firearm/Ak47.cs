using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Server.Worlds.Items.Firearm
{
    public class Ak47 : Firearm
    {
        public Ak47(long id, int magazine) : base(id, magazine)
        {

        }

        public override FirearmType FirearmType()
        {
            return Items.Firearm.FirearmType.Ak47;
        }

        public override int MagazineSize()
        {
            return 30;
        }

        public override StackableItem AmmoType()
        {
            return StackableItem.Ammo762X39;
        }

        public override float Distance()
        {
            return 100;
        }

        public override int Damage()
        {
            return 25;
        }

        public override float FireInterval()
        {
            return 0.1f;
        }

        public override float ReloadTime()
        {
            return 2.5f;
        }

        public override SoundType ShotSound()
        {
            return SoundType.Ak47Shot;
        }

        public override SoundType MisfireSound()
        {
            return SoundType.Ak47Misfire;
        }

        public override SoundType ReloadSound()
        {
            return SoundType.Ak47Reload;
        }
    }
}
