using System;
using Shooter.Infrastructure;
using Unity.Collections;
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

        private static int shotMask;

        private readonly ShotCadence cadence = new();
        private Hands hands;
        private InventoryHolder inventoryHolder;
        private Interactor interactor;
        private Inventory inventory;

        private bool triggerHeld;

        public event Action<FirearmAsset, ShotHit> Shot;

        public event Action<ShotTrace> ShotTraced;

        public event Action<FirearmAsset> Misfired;

        public event Action<FirearmAsset> ReloadStarted;

        private static int ShotMask => shotMask != 0 ? shotMask : shotMask = Interactor.LookMask | LayerMask.GetMask("Water");

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
                Log.Info($"Entity {name} pulled the trigger of an empty {firearm.AssetId}");
                triggerHeld = false;
                Misfired?.Invoke(asset);
                return false;
            }

            int shotIndex = cadence.Fire(asset, Time.time);
            ShotHit hit = TraceShot(asset, shotIndex);
            Shot?.Invoke(asset, hit);
            SendTrace(asset, hit);
            return true;
        }

        private void SendTrace(FirearmAsset asset, ShotHit hit)
        {
            Vector3 end = hit.IsMiss ? hit.Origin + hit.Direction * asset.Distance : hit.Point;
            SoundAsset impactSound = hit.IsMiss || asset.ImpactSounds == null ? null : asset.ImpactSounds.GetSoundOn(hit.Surface);

            ShotTracedRpc(hit.Origin, end, impactSound == null ? default : impactSound.NetworkId, impactSound == null ? (byte)0 : impactSound.PickRandomVariant());
        }

        [Rpc(SendTo.Everyone)]
        private void ShotTracedRpc(Vector3 origin, Vector3 end, FixedString32Bytes impactSoundId, byte impactSoundVariant)
        {
            SoundCatalog soundCatalog = Catalogs.GetCatalog<SoundCatalog>();
            SoundAsset impactSound = impactSoundId.IsEmpty || soundCatalog == null ? null : soundCatalog.GetAssetById(impactSoundId);

            ShotTraced?.Invoke(new ShotTrace(origin, end, impactSound, impactSoundVariant));
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

            Vector3 eyePosition = interactor.EyePosition;
            int hitCount = Interactor.CollectHits(eyePosition, shotDirection, asset.Distance, ShotMask, transform, ShotHits);
            if (!Interactor.TryFindNearestHit(ShotHits, hitCount, out RaycastHit hit))
            {
                Log.Info($"Entity {name} shot and missed");
                return new ShotHit(eyePosition, shotDirection);
            }

            PhysicsMaterial surface = Surface.FindAt(hit);
            Health victim = hit.collider.GetComponentInParent<Health>();
            if (victim == null)
            {
                Log.Info($"Entity {name} shot {hit.collider.name} at {hit.distance:0.0} m, surface {(surface == null ? "none" : surface.name)}");
                return new ShotHit(eyePosition, shotDirection, hit.collider, hit.point, hit.normal, surface, null, BodyPart.Torso);
            }

            BodyPart hitPart = FindWeakestHitBodyPart(hitCount, victim);
            Log.Info($"Entity {name} shot {victim.name} in {hitPart} at {hit.distance:0.0} m");
            return new ShotHit(eyePosition, shotDirection, hit.collider, hit.point, hit.normal, surface, victim, hitPart);
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
