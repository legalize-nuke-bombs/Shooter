using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Combat
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Interactor))]
    [RequireComponent(typeof(Hands))]
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(EarSpeaker))]
    [RequireComponent(typeof(MainRestrainable))]
    public class Gunner : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly RaycastHit[] Shots = new RaycastHit[32];
        private EarSpeaker earSpeaker;
        private Hands hands;

        private CharacterId id;
        private Interactor interactor;
        private Inventory inventory;
        private float lastShotAt;
        private MainRestrainable restrainable;
        private Speaker speaker;

        private int sprayShot;
        private bool triggerHeld;

        private void Awake()
        {
            id = GetComponent<CharacterId>();
            inventory = GetComponent<Inventory>();
            interactor = GetComponent<Interactor>();
            hands = GetComponent<Hands>();
            speaker = GetComponent<Speaker>();
            earSpeaker = GetComponent<EarSpeaker>();
            restrainable = GetComponent<MainRestrainable>();
        }

        private void Update()
        {
            if (!IsServer || !triggerHeld) return;

            var firearm = inventory.Equipped() as Firearm;
            if (firearm == null) return;

            FirearmSpec spec = Catalogs.Of<ItemCatalog>().Firearm(firearm.SpecId);
            if (spec == null || spec.FireMode != FireMode.Auto) return;

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
            if (!Ready(out Firearm firearm, out FirearmSpec spec, ActionType.Shoot)) return false;

            if (hands != null && !hands.TryTake(HandsAction.Shooting, spec.FireInterval, false, null)) return false;

            restrainable.RegisterAction(ActionType.Shoot, MainRestrainable.InstantAction);

            if (!firearm.Spend())
            {
                triggerHeld = false;
                speaker?.Play(spec.MisfireSound);
                return false;
            }

            if (Time.time - lastShotAt >= spec.SprayRecovery) sprayShot = 0;

            speaker?.Play(spec.ShotSound);
            Hit(spec);

            sprayShot++;
            lastShotAt = Time.time;
            return true;
        }

        public bool TryReload()
        {
            if (!Ready(out Firearm firearm, out FirearmSpec spec, ActionType.Reload)) return false;
            if (spec.Ammo == null) return false;

            int absent = spec.MagazineSize - firearm.Magazine;
            if (absent <= 0 || inventory.StackableAmount(spec.Ammo) == 0) return false;

            if (hands != null && !hands.TryTake(HandsAction.Reloading, spec.ReloadTime, true,
                    () => Reloaded(firearm, spec, absent))) return false;

            restrainable.RegisterAction(ActionType.Reload, MainRestrainable.InstantAction);
            speaker?.Play(spec.ReloadSound);
            Log.Info($"Entity {name} started reload of {firearm.SpecId}, {spec.ReloadTime}s");
            return true;
        }

        private bool Ready(out Firearm firearm, out FirearmSpec spec, ActionType action)
        {
            firearm = null;
            spec = null;

            if (!IsServer || !restrainable.CanPerform(action, MainRestrainable.InstantAction)) return false;

            firearm = inventory.Equipped() as Firearm;
            if (firearm == null) return false;

            spec = Catalogs.Of<ItemCatalog>().Firearm(firearm.SpecId);
            return spec != null;
        }

        private void Reloaded(Firearm firearm, FirearmSpec spec, int absent)
        {
            if (!inventory.Contains(firearm)) return;

            int taken = inventory.RemoveStackable(spec.Ammo, absent, InventoryOnConflict.Partly);
            firearm.Reload(taken, spec.MagazineSize);
            Log.Info(
                $"Entity {name} reloaded {firearm.SpecId} with {taken} rounds, {inventory.StackableAmount(spec.Ammo)} left in bag");
        }

        private void Hit(FirearmSpec spec)
        {
            Vector2 offset = spec.Spray.At(sprayShot);
            Vector3 direction = Quaternion.LookRotation(interactor.Sight) * Quaternion.Euler(-offset.y, offset.x, 0f) *
                                Vector3.forward;

            int found = Interactor.Look(interactor.Eyes, direction, spec.Distance, transform, Shots);
            if (!Interactor.TryNearest(Shots, found, out RaycastHit hit))
            {
                Log.Info($"Shot of entity {name} missed");
                return;
            }

            Health health = hit.collider.GetComponentInParent<Health>();
            if (health == null)
            {
                BulletHoles.Current.Add(hit.point, hit.normal);
                Log.Info($"Shot of entity {name} hit {hit.collider.name} without health");
                return;
            }

            BodyPart part = Weakest(found, health);
            if (part == BodyPart.Head) earSpeaker.Play(spec.HeadshotSound);
            int damage = Mathf.RoundToInt(spec.Damage * part.Multiplier());

            health.Damage(damage, id == null ? null : id.Value, spec.DamageType);
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
                if (part.Multiplier() <= highest) continue;

                weakest = part;
                highest = part.Multiplier();
            }

            return weakest;
        }
    }
}
