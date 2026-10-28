using System;
using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(IncrementalId))]
    public class Pickupable : NetworkBehaviour, IUsable, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<FixedString32Bytes> itemId = new();

        private ItemAsset item;
        private GameObject model;

        public event Action<Pickupable, NetworkObject> PickedUp;

        public void AssignItem(ItemAsset itemAsset)
        {
            itemId.Value = itemAsset.NetworkId;
            ShowItem(itemId.Value);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) itemId.OnValueChanged += HandleItemIdReplicated;

            ShowItem(itemId.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) itemId.OnValueChanged -= HandleItemIdReplicated;
        }

        public UsageType Usage => UsageType.PickUp;
        public void Use(NetworkObject user, out string promptResult)
        {
            Log.Info($"Entity {name} has been picked up by {user.name}");
            if (user.TryGetComponent(out Inventory inventory))
            {
                GiveItemTo(inventory);
            }
            else
            {
                Log.Warn($"Failed to find inventory of user {user.name}");
            }
            PickedUp?.Invoke(this, user);
            NetworkObject.Despawn(!NetworkObject.InScenePlaced);
            gameObject.SetActive(false);
            promptResult = $"You picked up {item.Id}";
        }

        public DigestionPriority Priority => DigestionPriority.High;
        public string Digest(DigestionDetail detail)
        {
            return $"Can be picked up ({item.Id})";
        }

        private void GiveItemTo(Inventory inventory)
        {
            switch (item)
            {
                case StackableItemAsset stackable:
                    inventory.AddStackable(stackable, 1);
                    break;
                case UniqueItemAsset unique:
                    inventory.AddUnique(unique.CreateItem());
                    break;
            }
        }

        private void HandleItemIdReplicated(FixedString32Bytes previousItemId, FixedString32Bytes currentItemId)
        {
            ShowItem(currentItemId);
        }

        private void ShowItem(FixedString32Bytes id)
        {
            if (model != null || id.IsEmpty) return;

            item = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(id);
            if (item == null || item.Model == null)
            {
                Log.Error($"Entity {name} cannot find item {id} or its model, stays invisible");
                return;
            }

            model = Instantiate(item.Model, transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            Log.Info($"Entity {name} lies as {item.Id}");
        }
    }
}
