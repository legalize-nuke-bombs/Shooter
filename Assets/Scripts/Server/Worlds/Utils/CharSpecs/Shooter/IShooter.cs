using Shooter.Server.Worlds.Utils.CharSpecs.InventoryKeeper;

namespace Shooter.Server.Worlds.Utils.CharSpecs.Shooter
{
    public interface IShooter
    {
        public bool CanShoot(IInventoryKeeper inventoryKeeper);
        public bool TryToShot(IInventoryKeeper inventoryKeeper);
    }
}
