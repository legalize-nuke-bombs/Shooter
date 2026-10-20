using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Health))]
    public class CorpseLeaverOnDeath : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject corpsePrefab;

        private Health health;

        protected override void ShooterAwake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            health.Died += LeaveCorpse;
        }

        private void OnDisable()
        {
            health.Died -= LeaveCorpse;
        }

        private void LeaveCorpse()
        {
            if (corpsePrefab == null)
            {
                Log.Error($"Entity {name} died without a corpse prefab");
                return;
            }

            GameObject corpseObject = NetworkSpawner.InstantiateAndSpawn(corpsePrefab, transform.position, transform.rotation);
            if (corpseObject == null)
            {
                Log.Error($"Entity {name} failed to spawn corpse");
                return;
            }

            Corpse corpse = corpseObject.GetComponent<Corpse>();
            if (corpse != null)
            {
                Skin skin = GetComponent<Skin>();
                if (skin != null && skin.Asset != null) corpse.AssignSkinAsset(skin.Asset);

                AssetNameable assetNameable = GetComponent<AssetNameable>();
                if (assetNameable != null && assetNameable.Asset != null) corpse.AssignNameAsset(assetNameable.Asset);
            }

            corpseObject.GetComponent<Lootable>()?.TakeAllItemsFrom(GetComponent<Inventory>());
            Log.Info($"Entity {name} left a corpse at {transform.position}");
        }
    }
}
