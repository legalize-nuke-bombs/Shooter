using Shooter.Infrastructure;
using Unity.Netcode;

namespace Shooter.Game
{
    public class Lootable : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public void Fill(Inventory from)
        {
            if (!IsServer || from == null) return;

            Inventory own = GetComponent<Inventory>();
            if (own == null)
            {
                Log.Warn($"Lootable {name} has no inventory to take the belongings");
                return;
            }

            from.DrainInto(own);
            Log.Info($"Lootable {name} took the belongings");
        }
    }
}
