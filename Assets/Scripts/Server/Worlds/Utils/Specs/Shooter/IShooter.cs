using Shooter.Server.Worlds.Utils.Specs.InventoryKeeper;

namespace Shooter.Server.Worlds.Utils.Specs.Shooter
{
    public interface IShooter
    {
        public bool CanShoot(IInventoryKeeper inventoryKeeper);
        public bool TryToShot(IInventoryKeeper inventoryKeeper);
    }
}
