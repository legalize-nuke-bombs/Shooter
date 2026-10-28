using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(IncrementalId))]
    public class Lootable : NetworkBehaviour, IUsable
    {
        public const string ShownNameField = nameof(shownName);
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private string shownName;

        public string ShownName => string.IsNullOrEmpty(shownName) ? name : shownName;

        public UsageType Usage => UsageType.Loot;

        public void Use(NetworkObject user, out string promptResult)
        {
            if (user.TryGetComponent(out Player _) && user.TryGetComponent(out InventoryExchanger exchanger))
            {
                exchanger.OpenLoot(this);
                promptResult = $"{name} is opened";
                return;
            }

            promptResult = "Look into it with look_by_ids in detailed mode, and exchange things with it with exchange_stackable and exchange_unique";
        }

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
