using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(InventoryHolder))]
    [RequireComponent(typeof(Interactor))]
    [RequireComponent(typeof(Hands))]
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(EarSpeaker))]
    public class Gunner : ShooterNetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly RaycastHit[] ShotHits = new RaycastHit[32];
        private readonly ShotCadence cadence = new();
        private BulletHoles bulletHoles;
        private EarSpeaker earSpeaker;
        private Hands hands;
        private InventoryHolder inventoryHolder;

        private Character character;
        private Interactor interactor;
        private Inventory inventory;
        private Speaker speaker;

        private bool triggerHeld;

        protected override void ShooterAwake()
        {
            bulletHoles = Registers.Get<BulletHoles>();
            character = GetComponent<Character>();
            inventory = GetComponent<Inventory>();
            inventoryHolder = GetComponent<InventoryHolder>();
            interactor = GetComponent<Interactor>();
            hands = GetComponent<Hands>();
            speaker = GetComponent<Speaker>();
            earSpeaker = GetComponent<EarSpeaker>();
        }

        private void Update()
        {
            if (!IsServer || !triggerHeld) return;

            TryShoot(true);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void PressTriggerRpc()
        {
            triggerHeld = true;
            TryShoot(false);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ReleaseTriggerRpc()
        {
            triggerHeld = false;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ReloadRpc()
        {
            TryReload();
        }

        private bool TryShoot(bool sustained)
        {
            if (!TryGetHeldFirearm(out Firearm firearm, out FirearmAsset asset)) return false;
            if (!cadence.CanFire(asset, sustained, Time.time)) return false;

            if (!hands.TryStartAction(ActionType.Shoot, asset.FireInterval, false, null)) return false;

            if (!firearm.TrySpendRound())
            {
                triggerHeld = false;
                speaker?.PlayForEveryone(asset.MisfireSound);
                return false;
            }

            int shotIndex = cadence.Fire(asset, Time.time);
            speaker?.PlayForEveryone(asset.ShotSound);
            TraceShotAndApplyHit(asset, shotIndex);
            return true;
        }

        public bool TryReload()
        {
            if (!TryGetHeldFirearm(out Firearm firearm, out FirearmAsset asset)) return false;
            if (asset.Ammo == null) return false;

            int missingRounds = asset.MagazineSize - firearm.RoundsInMagazine;
            if (missingRounds <= 0 || inventory.CountStackable(asset.Ammo) == 0) return false;

            if (!hands.TryStartAction(ActionType.Reload, asset.ReloadTime, true, () => FinishReload(firearm, asset, missingRounds))) return false;

            speaker?.PlayForEveryone(asset.ReloadSound);
            Log.Info($"Entity {name} started reload of {firearm.AssetId}, {asset.ReloadTime}s");
            return true;
        }

        private bool TryGetHeldFirearm(out Firearm firearm, out FirearmAsset asset)
        {
            firearm = null;
            asset = null;

            if (!IsServer) return false;

            firearm = inventoryHolder.HeldItem as Firearm;
            if (firearm == null) return false;

            asset = Catalogs.GetCatalog<ItemCatalog>().GetFirearmAssetById(firearm.AssetId);
            return asset != null;
        }

        private void FinishReload(Firearm firearm, FirearmAsset asset, int missingRounds)
        {
            if (!inventory.ContainsUnique(firearm)) return;

            int takenRounds = inventory.RemoveStackable(asset.Ammo, missingRounds, InventoryShortagePolicy.Partly);
            firearm.LoadRounds(takenRounds, asset.MagazineSize);
            Log.Info($"Entity {name} reloaded {firearm.AssetId} with {takenRounds} rounds, {inventory.CountStackable(asset.Ammo)} left in bag");
        }

        private void TraceShotAndApplyHit(FirearmAsset asset, int shotIndex)
        {
            Vector2 sprayOffset = asset.Spray.GetOffsetForShot(shotIndex);
            Vector3 shotDirection = Quaternion.LookRotation(interactor.LookDirection) * Quaternion.Euler(-sprayOffset.y, sprayOffset.x, 0f) * Vector3.forward;

            int hitCount = Interactor.CollectLookHits(interactor.EyePosition, shotDirection, asset.Distance, transform, ShotHits);
            if (!Interactor.TryFindNearestHit(ShotHits, hitCount, out RaycastHit hit))
            {
                Log.Info($"Shot of entity {name} missed");
                return;
            }

            Health health = hit.collider.GetComponentInParent<Health>();
            if (health == null)
            {
                bulletHoles.AddHole(hit.point, hit.normal);
                Log.Info($"Shot of entity {name} hit {hit.collider.name} without health");
                return;
            }

            BodyPart hitPart = FindWeakestHitBodyPart(hitCount, health);
            if (hitPart == BodyPart.Head) earSpeaker.PlayForOwner(asset.HeadshotSound);
            int damage = Mathf.RoundToInt(asset.Damage * hitPart.GetDamageMultiplier());

            health.TakeDamage(damage, character, asset.DamageType);
            Log.Info($"Shot of entity {name} hit {health.name} in {hitPart} for {damage} damage");
        }

        private BodyPart FindWeakestHitBodyPart(int hitCount, Health victim)
        {
            BodyPart weakestPart = BodyPart.Torso;
            float highestMultiplier = 0f;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = ShotHits[i].collider;
                if (hitCollider.GetComponentInParent<Health>() != victim) continue;

                Hitbox hitbox = hitCollider.GetComponent<Hitbox>();
                BodyPart part = hitbox == null ? BodyPart.Torso : hitbox.Part;
                if (part.GetDamageMultiplier() <= highestMultiplier) continue;

                weakestPart = part;
                highestMultiplier = part.GetDamageMultiplier();
            }

            return weakestPart;
        }
    }
}
