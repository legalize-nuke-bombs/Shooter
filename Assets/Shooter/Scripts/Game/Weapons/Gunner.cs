using System.Linq;
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
    [RequireComponent(typeof(MainRestrainable))]
    public class Gunner : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private static readonly RaycastHit[] ShotHits = new RaycastHit[32];
        private BulletHoles bulletHoles;
        private EarSpeaker earSpeaker;
        private Hands hands;
        private InventoryHolder inventoryHolder;

        private Character character;
        private Interactor interactor;
        private Inventory inventory;
        private float lastShotAt;
        private MainRestrainable restrainable;
        private Speaker speaker;

        private int shotIndexInSpray;
        private bool triggerHeld;

        private void Awake()
        {
            bulletHoles = Registers.GetAll<BulletHoles>(RegisterInactivePolicy.Include).Single();
            character = GetComponent<Character>();
            inventory = GetComponent<Inventory>();
            inventoryHolder = GetComponent<InventoryHolder>();
            interactor = GetComponent<Interactor>();
            hands = GetComponent<Hands>();
            speaker = GetComponent<Speaker>();
            earSpeaker = GetComponent<EarSpeaker>();
            restrainable = GetComponent<MainRestrainable>();
        }

        private void Update()
        {
            if (!IsServer || !triggerHeld) return;

            Firearm firearm = inventoryHolder.HeldItem as Firearm;
            if (firearm == null) return;

            FirearmAsset asset = Catalogs.GetCatalog<ItemCatalog>().GetFirearmAssetById(firearm.AssetId);
            if (asset == null || asset.FireMode != FireMode.Auto) return;

            TryShoot();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void PressTriggerRpc()
        {
            triggerHeld = true;
            TryShoot();
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

        public bool TryShoot()
        {
            if (!TryGetHeldFirearmForAction(out Firearm firearm, out FirearmAsset asset, ActionType.Shoot)) return false;

            if (hands != null && !hands.TryStartAction(HandsAction.Shooting, asset.FireInterval, false, null)) return false;

            restrainable.RegisterAction(ActionType.Shoot, MainRestrainable.InstantActionDuration);

            if (!firearm.TrySpendRound())
            {
                triggerHeld = false;
                speaker?.PlayForEveryone(asset.MisfireSound);
                return false;
            }

            if (Time.time - lastShotAt >= asset.SprayRecovery) shotIndexInSpray = 0;

            speaker?.PlayForEveryone(asset.ShotSound);
            TraceShotAndApplyHit(asset);

            shotIndexInSpray++;
            lastShotAt = Time.time;
            return true;
        }

        public bool TryReload()
        {
            if (!TryGetHeldFirearmForAction(out Firearm firearm, out FirearmAsset asset, ActionType.Reload)) return false;
            if (asset.Ammo == null) return false;

            int missingRounds = asset.MagazineSize - firearm.RoundsInMagazine;
            if (missingRounds <= 0 || inventory.CountStackable(asset.Ammo) == 0) return false;

            if (hands != null && !hands.TryStartAction(HandsAction.Reloading, asset.ReloadTime, true, () => FinishReload(firearm, asset, missingRounds))) return false;

            restrainable.RegisterAction(ActionType.Reload, MainRestrainable.InstantActionDuration);
            speaker?.PlayForEveryone(asset.ReloadSound);
            Log.Info($"Entity {name} started reload of {firearm.AssetId}, {asset.ReloadTime}s");
            return true;
        }

        private bool TryGetHeldFirearmForAction(out Firearm firearm, out FirearmAsset asset, ActionType action)
        {
            firearm = null;
            asset = null;

            if (!IsServer || !restrainable.CanPerform(action, MainRestrainable.InstantActionDuration)) return false;

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

        private void TraceShotAndApplyHit(FirearmAsset asset)
        {
            Vector2 sprayOffset = asset.Spray.GetOffsetForShot(shotIndexInSpray);
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
