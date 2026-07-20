using Shooter.Server.Worlds.Utils.CharSpecs.InventoryKeeper;

namespace Shooter.Server.Worlds.Utils.CharSpecs.Shooter
{
    public class NotAShooter : IShooter
    {
        public bool CanShoot(IInventoryKeeper inventoryKeeper)
        {
            return false;
        }

        public bool TryToShot(IInventoryKeeper inventoryKeeper)
        {
            return false;
        }
    }
}
