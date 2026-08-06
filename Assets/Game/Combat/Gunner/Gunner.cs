using Shooter.Game.Body;
using Shooter.Game.Body.Hitboxes;
using Shooter.Game.Body.Sounding;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Combat
{
    [RequireComponent(typeof(Nameable))]
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Interactor))]
    [RequireComponent(typeof(Hands))]
    [RequireComponent(typeof(Speaker))]
    public class Gunner : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly RaycastHit[] Shots = new RaycastHit[32];

        private Nameable nameable;
        private Inventory inventory;
        private Interactor interactor;
        private Hands hands;
        private Speaker speaker;
        private IRestraint[] restraints;

        private void Awake()
        {
            nameable = GetComponent<Nameable>();
            inventory = GetComponent<Inventory>();
            interactor = GetComponent<Interactor>();
            hands = GetComponent<Hands>();
            speaker = GetComponent<Speaker>();
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
            if (!Ready(out Item item, out FirearmSpec spec)) return false;

            if (hands != null && !hands.TryTake(HandsAction.Shooting, spec.FireInterval, false, null)) return false;

            if (item.State <= 0)
            {
                speaker?.Play(spec.MisfireSound);
                return false;
            }

            inventory.Reequip(new Item(item.Id, item.Amount, item.State - 1));
            speaker?.Play(spec.ShotSound);
            Hit(spec);
            return true;
        }

        public bool TryReload()
        {
            if (!Ready(out Item item, out FirearmSpec spec)) return false;
            if (spec.Ammo == null) return false;

            int absent = spec.MagazineSize - item.State;
            if (absent <= 0 || inventory.Amount(spec.Ammo.Id) == 0) return false;

            if (hands != null && !hands.TryTake(HandsAction.Reloading, spec.ReloadTime, true, () => Reloaded(spec, absent))) return false;

            speaker?.Play(spec.ReloadSound);
            Log.Info("Entity {} started reload of {}, {}s", name, item.Id, spec.ReloadTime);
            return true;
        }

        private bool Ready(out Item item, out FirearmSpec spec)
        {
            item = default;
            spec = null;

            if (!IsServer || Restraints.Any(restraints)) return false;
            if (!inventory.Equipped(out item)) return false;

            spec = inventory.Catalog == null ? null : inventory.Catalog.Firearm(item.Id);
            return spec != null;
        }

        private void Reloaded(FirearmSpec spec, int absent)
        {
            if (!inventory.Equipped(out Item item)) return;

            int taken = inventory.Remove(spec.Ammo.Id, absent, InventoryOnConflict.Partly);
            inventory.Reequip(new Item(item.Id, item.Amount, item.State + taken));
            Log.Info("Entity {} reloaded {} with {} rounds, {} left in bag", name, item.Id, taken, inventory.Amount(spec.Ammo.Id));
        }

        private void Hit(FirearmSpec spec)
        {
            int found = interactor.Look(spec.Distance, Shots);
            if (!Interactor.TryNearest(Shots, found, out RaycastHit hit))
            {
                Log.Info("Shot of entity {} missed", name);
                return;
            }

            var health = hit.collider.GetComponentInParent<Health>();
            if (health == null)
            {
                Log.Info("Shot of entity {} hit {} without health", name, hit.collider.name);
                return;
            }

            BodyPart part = Weakest(found, health);
            if (part == BodyPart.Head)
            {
                if (health.TryGetComponent(out Speaker targetSpeaker))
                {
                    targetSpeaker.Play(spec.HeadshotSound);
                }
            }
            int damage = Mathf.RoundToInt(spec.Damage * part.Multiplier());

            health.Damage(damage, nameable.PromptName());
            Log.Info("Shot of entity {} hit {} in {} for {} damage", name, health.name, part, damage);
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
