using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(InventoryHolder))]
    [RequireComponent(typeof(Interactor))]
    [RequireComponent(typeof(Hands))]
    public class Gunner : ShooterNetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly RaycastHit[] ShotHits = new RaycastHit[32];
        private readonly ShotCadence cadence = new();
        private Hands hands;
        private InventoryHolder inventoryHolder;
        private Interactor interactor;
        private Inventory inventory;

        private bool triggerHeld;

        public event Action<FirearmAsset, ShotHit> Shot;

        public event Action<FirearmAsset> Misfired;

        public event Action<FirearmAsset> ReloadStarted;

        protected override void ShooterAwake()
        {
            inventory = GetComponent<Inventory>();
            inventoryHolder = GetComponent<InventoryHolder>();
            interactor = GetComponent<Interactor>();
            hands = GetComponent<Hands>();
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
                Misfired?.Invoke(asset);
                return false;
            }

            int shotIndex = cadence.Fire(asset, Time.time);
            Shot?.Invoke(asset, TraceShot(asset, shotIndex));
            return true;
        }

        public bool TryReload()
        {
            if (!TryGetHeldFirearm(out Firearm firearm, out FirearmAsset asset)) return false;
            if (asset.Ammo == null) return false;

            int missingRounds = asset.MagazineSize - firearm.RoundsInMagazine;
            if (missingRounds <= 0 || inventory.CountStackable(asset.Ammo) == 0) return false;

            if (!hands.TryStartAction(ActionType.Reload, asset.ReloadTime, true, () => FinishReload(firearm, asset, missingRounds))) return false;

            Log.Info($"Entity {name} started reload of {firearm.AssetId}, {asset.ReloadTime}s");
            ReloadStarted?.Invoke(asset);
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

        private ShotHit TraceShot(FirearmAsset asset, int shotIndex)
        {
            Vector2 sprayOffset = asset.Spray.GetOffsetForShot(shotIndex);
            Vector3 shotDirection = Quaternion.LookRotation(interactor.LookDirection) * Quaternion.Euler(-sprayOffset.y, sprayOffset.x, 0f) * Vector3.forward;

            int hitCount = Interactor.CollectLookHits(interactor.EyePosition, shotDirection, asset.Distance, transform, ShotHits);
            if (!Interactor.TryFindNearestHit(ShotHits, hitCount, out RaycastHit hit))
            {
                Log.Info($"Shot of entity {name} missed");
                return default;
            }

            Health victim = hit.collider.GetComponentInParent<Health>();
            if (victim == null)
            {
                Log.Info($"Shot of entity {name} hit {hit.collider.name} without health");
                return new ShotHit(hit.collider, hit.point, hit.normal, null, BodyPart.Torso);
            }

            BodyPart hitPart = FindWeakestHitBodyPart(hitCount, victim);
            Log.Info($"Shot of entity {name} hit {victim.name} in {hitPart}");
            return new ShotHit(hit.collider, hit.point, hit.normal, victim, hitPart);
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
