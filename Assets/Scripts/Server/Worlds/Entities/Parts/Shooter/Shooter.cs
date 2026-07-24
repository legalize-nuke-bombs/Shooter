using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Protocol;
using Shooter.Server.Worlds.Entities.Parts.Hands;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Items;
using Shooter.Server.Worlds.Items.Firearm;

namespace Shooter.Server.Worlds.Entities.Parts.Shooter
{
    public sealed class Shooter : Part
    {
        private readonly Gaze gaze;

        public Shooter(Entity self, Gaze gaze) : base(self, typeof(Shooter))
        {
            this.gaze = gaze;
        }

        public override void Apply(PlayerIntent input)
        {
            if (!input.Shoot && !input.Reload) return;
            if (!Ready()) return;

            if (input.Shoot) TryShoot(input.Pitch, input.Yaw);
            if (input.Reload) TryReload();
        }

        public bool TryShoot(float pitch, float yaw)
        {
            Inventory.Inventory inventory = Self.Get<Inventory.Inventory>();
            Hands.Hands hands = Self.Get<Hands.Hands>();
            if (inventory == null || hands == null) return false;

            if (!(inventory.Equipped() is Firearm firearm)) return false;

            if (!hands.TryTake(HandsAction.Shooting, firearm.FireInterval, false, null)) return false;

            Speaker.Speaker speaker = Self.Get<Speaker.Speaker>();

            if (!firearm.TryShoot())
            {
                speaker?.Play(firearm.MisfireSound);
                return false;
            }

            speaker?.Play(firearm.ShotSound);
            Shot(pitch, yaw, firearm);
            return true;
        }

        public bool TryReload()
        {
            Inventory.Inventory inventory = Self.Get<Inventory.Inventory>();
            Hands.Hands hands = Self.Get<Hands.Hands>();
            if (inventory == null || hands == null) return false;

            if (!(inventory.Equipped() is Firearm firearm)) return false;

            if (firearm.MagazineFull || inventory.Amount(firearm.AmmoType) == 0) return false;

            if (!hands.TryTake(HandsAction.Reloading, firearm.ReloadTime, true, () => Reloaded(inventory, firearm))) return false;

            Self.Get<Speaker.Speaker>()?.Play(firearm.ReloadSound);
            Log.Info("Entity {} started reload of {}, {}s", Self.Name, firearm.FirearmType, firearm.ReloadTime);
            return true;
        }

        private bool Ready()
        {
            Health.Health health = Self.Get<Health.Health>();
            if (health != null && !health.Alive) return false;

            Sleeper.Sleeper sleeper = Self.Get<Sleeper.Sleeper>();
            return sleeper == null || !sleeper.Sleeping;
        }

        private void Reloaded(Inventory.Inventory inventory, Firearm firearm)
        {
            StackableItem ammoType = firearm.AmmoType;
            int spent = firearm.Reload(inventory.Amount(ammoType));
            inventory.Remove(ammoType, spent, InventoryOnConflictAction.Partly);
            Log.Info("Entity {} reloaded {} with {} rounds, {} {} left", Self.Name, firearm.FirearmType, spent, inventory.Amount(ammoType), ammoType);
        }

        private void Shot(float pitch, float yaw, Firearm firearm)
        {
            Vector3 from = Self.Position;

            if (!gaze.TryLook(from, pitch, yaw, firearm.Distance, out RaycastHit hit))
            {
                Log.Info("Shot of entity {} from {} missed", Self.Name, from);
                return;
            }

            Entity target = gaze.Resolve(hit);
            if (target == null)
            {
                Log.Info("Shot of entity {} from {} hit map at {}", Self.Name, from, hit.point);
                return;
            }

            Health.Health health = target.Get<Health.Health>();
            if (health == null)
            {
                Log.Info("Shot of entity {} from {} hit entity {} without health", Self.Name, from, target.Name);
                return;
            }

            health.Damage(firearm.Damage);
            Log.Info("Shot of entity {} from {} hit entity {} for {} damage", Self.Name, from, target.Name, firearm.Damage);
        }
    }
}
