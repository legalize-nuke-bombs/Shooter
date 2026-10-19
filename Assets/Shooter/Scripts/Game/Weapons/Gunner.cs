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

        private static readonly RaycastHit[] Shots = new RaycastHit[32];
        private BulletHoles bulletHoles;
        private EarSpeaker earSpeaker;
        private Hands hands;
        private InventoryHolder holder;

        private Character character;
        private Interactor interactor;
        private Inventory inventory;
        private float lastShotAt;
        private MainRestrainable restrainable;
        private Speaker speaker;

        private int sprayShot;
        private bool triggerHeld;

        private void Awake()
        {
            bulletHoles = Registers.GetAll<BulletHoles>(RegisterInactivePolicy.Include).Single();
            character = GetComponent<Character>();
            inventory = GetComponent<Inventory>();
            holder = GetComponent<InventoryHolder>();
            interactor = GetComponent<Interactor>();
            hands = GetComponent<Hands>();
            speaker = GetComponent<Speaker>();
            earSpeaker = GetComponent<EarSpeaker>();
            restrainable = GetComponent<MainRestrainable>();
        }

        private void Update()
        {
            if (!IsServer || !triggerHeld) return;

            var firearm = holder.Held as Firearm;
            if (firearm == null) return;

            FirearmAsset asset = Catalogs.GetCatalog<ItemCatalog>().Firearm(firearm.AssetId);
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
            if (!Ready(out Firearm firearm, out FirearmAsset asset, ActionType.Shoot)) return false;

            if (hands != null && !hands.TryStartAction(HandsAction.Shooting, asset.FireInterval, false, null)) return false;

            restrainable.RegisterAction(ActionType.Shoot, MainRestrainable.InstantActionDuration);

            if (!firearm.Spend())
            {
                triggerHeld = false;
                speaker?.PlayForEveryone(asset.MisfireSound);
                return false;
            }

            if (Time.time - lastShotAt >= asset.SprayRecovery) sprayShot = 0;

            speaker?.PlayForEveryone(asset.ShotSound);
            Hit(asset);

            sprayShot++;
            lastShotAt = Time.time;
            return true;
        }

        public bool TryReload()
        {
            if (!Ready(out Firearm firearm, out FirearmAsset asset, ActionType.Reload)) return false;
            if (asset.Ammo == null) return false;

            int absent = asset.MagazineSize - firearm.Magazine;
            if (absent <= 0 || inventory.Count(asset.Ammo) == 0) return false;

            if (hands != null && !hands.TryStartAction(HandsAction.Reloading, asset.ReloadTime, true,
                    () => Reloaded(firearm, asset, absent))) return false;

            restrainable.RegisterAction(ActionType.Reload, MainRestrainable.InstantActionDuration);
            speaker?.PlayForEveryone(asset.ReloadSound);
            Log.Info($"Entity {name} started reload of {firearm.AssetId}, {asset.ReloadTime}s");
            return true;
        }

        private bool Ready(out Firearm firearm, out FirearmAsset asset, ActionType action)
        {
            firearm = null;
            asset = null;

            if (!IsServer || !restrainable.CanPerform(action, MainRestrainable.InstantActionDuration)) return false;

            firearm = holder.Held as Firearm;
            if (firearm == null) return false;

            asset = Catalogs.GetCatalog<ItemCatalog>().Firearm(firearm.AssetId);
            return asset != null;
        }

        private void Reloaded(Firearm firearm, FirearmAsset asset, int absent)
        {
            if (!inventory.Contains(firearm)) return;

            int taken = inventory.Remove(asset.Ammo, absent, InventoryOnConflict.Partly);
            firearm.Reload(taken, asset.MagazineSize);
            Log.Info(
                $"Entity {name} reloaded {firearm.AssetId} with {taken} rounds, {inventory.Count(asset.Ammo)} left in bag");
        }

        private void Hit(FirearmAsset asset)
        {
            Vector2 offset = asset.Spray.At(sprayShot);
            Vector3 direction = Quaternion.LookRotation(interactor.Sight) * Quaternion.Euler(-offset.y, offset.x, 0f) *
                                Vector3.forward;

            int found = Interactor.Look(interactor.Eyes, direction, asset.Distance, transform, Shots);
            if (!Interactor.TryNearest(Shots, found, out RaycastHit hit))
            {
                Log.Info($"Shot of entity {name} missed");
                return;
            }

            Health health = hit.collider.GetComponentInParent<Health>();
            if (health == null)
            {
                bulletHoles.Add(hit.point, hit.normal);
                Log.Info($"Shot of entity {name} hit {hit.collider.name} without health");
                return;
            }

            BodyPart part = Weakest(found, health);
            if (part == BodyPart.Head) earSpeaker.PlayForOwner(asset.HeadshotSound);
            int damage = Mathf.RoundToInt(asset.Damage * part.GetDamageMultiplier());

            health.TakeDamage(damage, character, asset.DamageType);
            Log.Info($"Shot of entity {name} hit {health.name} in {part} for {damage} damage");
        }

        private BodyPart Weakest(int found, Health victim)
        {
            BodyPart weakest = BodyPart.Torso;
            float highest = 0f;

            for (int i = 0; i < found; i++)
            {
                Collider collider = Shots[i].collider;
                if (collider.GetComponentInParent<Health>() != victim) continue;

                Hitbox hitbox = collider.GetComponent<Hitbox>();
                BodyPart part = hitbox == null ? BodyPart.Torso : hitbox.Part;
                if (part.GetDamageMultiplier() <= highest) continue;

                weakest = part;
                highest = part.GetDamageMultiplier();
            }

            return weakest;
        }
    }
}
