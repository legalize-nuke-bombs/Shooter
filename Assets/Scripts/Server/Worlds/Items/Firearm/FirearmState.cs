namespace Shooter.Server.Worlds.Items.Firearm
{
    public class FirearmState : UniqueItemState
    {
        public int Magazine { get; set; }
        public FirearmType FirearmType { get; set; }
        public int MagazineSize { get; set; }
        public StackableItem AmmoType { get; set; }
    }
}
