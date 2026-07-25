using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Items;
using Shooter.Logging;

namespace Shooter.Game.Dying
{
    public class Corpse : NetworkBehaviour
    {
        public void Fill(Inventory from)
        {
            if (!IsServer || from == null) return;

            var own = GetComponent<Inventory>();
            if (own == null)
            {
                Log.Warn("Corpse {} has no inventory to take the belongings", name);
                return;
            }

            from.DrainInto(own);
            Log.Info("Corpse {} took the belongings, {} slots", name, own.Count);
        }
    }
}
