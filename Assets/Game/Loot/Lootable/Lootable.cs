using Shooter.Logging;
using Unity.Netcode;

namespace Shooter.Game.Loot
{
    public class Lootable : NetworkBehaviour
    {
        public void Fill(Inventory from)
        {
            if (!IsServer || from == null) return;

            var own = GetComponent<Inventory>();
            if (own == null)
            {
                Log.Warn("Lootable {} has no inventory to take the belongings", name);
                return;
            }

            from.DrainInto(own);
            Log.Info("Lootable {} took the belongings, {} slots", name, own.Count);
        }
    }
}
