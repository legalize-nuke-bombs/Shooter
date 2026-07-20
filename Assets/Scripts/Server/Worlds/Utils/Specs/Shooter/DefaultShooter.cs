using Shooter.Server.Worlds.Utils.Specs.InventoryKeeper;
using Shooter.Server.Worlds.Utils.Items;
using Shooter.Server.Worlds.Utils.Items.Firearm;

namespace Shooter.Server.Worlds.Utils.Specs.Shooter
{
    public class DefaultShooter : IShooter
    {
        public bool CanShoot(IInventoryKeeper inventoryKeeper)
        {
            UniqueItem equipted = inventoryKeeper.Equipted();

            if (equipted == null)
            {
                return false;
            }

            if (equipted is Firearm firearm)
            {
                return firearm.CanShoot();
            }

            return false;
        }

        public bool TryToShot(IInventoryKeeper inventoryKeeper)
        {
            UniqueItem equipted = inventoryKeeper.Equipted();

            if (equipted == null)
            {
                return false;
            }

            if (equipted is Firearm firearm)
            {
                return firearm.TryToShot();
            }

            return false;
        }
    }
}
