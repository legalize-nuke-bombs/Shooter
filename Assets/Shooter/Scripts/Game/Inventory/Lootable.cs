using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public class Lootable : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public void TakeAllItemsFrom(Inventory sourceInventory)
        {
            if (!IsServer || sourceInventory == null) return;

            Inventory ownInventory = GetComponent<Inventory>();
            if (ownInventory == null)
            {
                Log.Error($"Lootable {name} has no inventory to take the belongings");
                return;
            }

            sourceInventory.DrainInto(ownInventory);
            Log.Info($"Lootable {name} took the belongings");
        }
    }
}
