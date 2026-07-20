namespace Shooter.Server.Worlds.Utils.Items.Firearm
{
    public class Ak47 : Firearm
    {
        public Ak47(long id, int magazine) : base(id, magazine)
        {

        }

        protected override FirearmType FirearmType()
        {
            return Items.Firearm.FirearmType.Ak47;
        }

        protected override int MagazineSize()
        {
            return 30;
        }

        protected override StackableItem AmmoType()
        {
            return StackableItem.Ammo762X39;
        }
    }
}
