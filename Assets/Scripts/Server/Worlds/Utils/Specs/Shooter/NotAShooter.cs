using Shooter.Server.Worlds.Utils.Specs.InventoryKeeper;

namespace Shooter.Server.Worlds.Utils.Specs.Shooter
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
