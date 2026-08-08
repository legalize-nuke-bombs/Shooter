using Shooter.Game.Body;
using Shooter.Game.Body.EarSounding;
using Shooter.Game.Body.Hitboxes;
using Shooter.Game.Body.Sounding;
using Shooter.Game.Identity;
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
    public class Gunner : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly RaycastHit[] Shots = new RaycastHit[32];

        private PersistentId id;
        private Inventory inventory;
        private Interactor interactor;
        private Hands hands;
        private Speaker speaker;
        private EarSpeaker earSpeaker;
        private IRestraint[] restraints;

        private void Awake()
        {
            id = GetComponent<PersistentId>();
            inventory = GetComponent<Inventory>();
            interactor = GetComponent<Interactor>();
            hands = GetComponent<Hands>();
            speaker = GetComponent<Speaker>();
            earSpeaker = GetComponent<EarSpeaker>();
            restraints = GetComponents<IRestraint>();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void FireRpc()
        {
            TryShoot();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ReloadRpc()
        {
            TryReload();
        }

        public bool TryShoot()
        {
            if (!Ready(out Firearm firearm, out FirearmSpec spec)) return false;

            if (hands != null && !hands.TryTake(HandsAction.Shooting, spec.FireInterval, false, null)) return false;

            if (!firearm.Spend())
            {
                speaker?.Play(spec.MisfireSound);
                return false;
            }

            speaker?.Play(spec.ShotSound);
            Hit(spec);
            return true;
        }

        public bool TryReload()
        {
            if (!Ready(out Firearm firearm, out FirearmSpec spec)) return false;
            if (spec.Ammo == null) return false;

            int absent = spec.MagazineSize - firearm.Magazine;
            if (absent <= 0 || inventory.StackableAmount(spec.Ammo) == 0) return false;

            if (hands != null && !hands.TryTake(HandsAction.Reloading, spec.ReloadTime, true, () => Reloaded(firearm, spec, absent))) return false;

            speaker?.Play(spec.ReloadSound);
            Log.Info($"Entity {name} started reload of {firearm.SpecId}, {spec.ReloadTime}s");
            return true;
        }

        private bool Ready(out Firearm firearm, out FirearmSpec spec)
        {
            firearm = null;
            spec = null;

            if (!IsServer || Restraints.Any(restraints)) return false;

            firearm = inventory.Equipped() as Firearm;
            if (firearm == null) return false;

            spec = Environment.Current.Items.Firearm(firearm.SpecId);
            return spec != null;
        }

        private void Reloaded(Firearm firearm, FirearmSpec spec, int absent)
        {
            if (!inventory.Contains(firearm)) return;

            int taken = inventory.RemoveStackable(spec.Ammo, absent, InventoryOnConflict.Partly);
            firearm.Reload(taken, spec.MagazineSize);
            Log.Info($"Entity {name} reloaded {firearm.SpecId} with {taken} rounds, {(inventory.StackableAmount(spec.Ammo))} left in bag");
        }

        private void Hit(FirearmSpec spec)
        {
            int found = interactor.Look(spec.Distance, Shots);
            if (!Interactor.TryNearest(Shots, found, out RaycastHit hit))
            {
                Log.Info($"Shot of entity {name} missed");
                return;
            }

            var health = hit.collider.GetComponentInParent<Health>();
            if (health == null)
            {
                Log.Info($"Shot of entity {name} hit {hit.collider.name} without health");
                return;
            }

            BodyPart part = Weakest(found, health);
            if (part == BodyPart.Head) earSpeaker.Play(spec.HeadshotSound);
            int damage = Mathf.RoundToInt(spec.Damage * part.Multiplier());

            health.Damage(damage, id == null ? null : id.Value);
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

                var hitbox = collider.GetComponent<Hitbox>();
                BodyPart part = hitbox == null ? BodyPart.Torso : hitbox.Part;
                if (part.Multiplier() <= highest) continue;

                weakest = part;
                highest = part.Multiplier();
            }

            return weakest;
        }
    }
}
