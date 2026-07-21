using System;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Inventory;
using Shooter.Server.Worlds.Entities.Parts.Hands;
using Shooter.Server.Worlds.Items;
using Shooter.Server.Worlds.Items.Firearm;
using UnityEngine;

namespace Shooter.Server.Worlds.Entities.Parts.Shooter
{
    public class Shooter : Part
    {
        private readonly Inventory.Inventory inventory;
        private readonly Speaker.Speaker speaker;
        private readonly Sight sight;
        private readonly WorldEntities worldEntities;
        private readonly Hands.Hands hands;

        public Shooter(Inventory.Inventory inventory, Speaker.Speaker speaker, Sight sight, WorldEntities worldEntities, Hands.Hands hands)
        {
            this.inventory = inventory;
            this.speaker = speaker;
            this.sight = sight;
            this.worldEntities = worldEntities;
            this.hands = hands;
        }

        public bool TryToShoot(Vector3 position, float pitch, float yaw)
        {
            if (!(inventory.Equipped() is Firearm firearm))
            {
                return false;
            }

            if (!hands.TryTake(HandsAction.Shooting, firearm.FireInterval(), false, null))
            {
                return false;
            }

            if (!firearm.TryToShoot())
            {
                speaker.Play(firearm.MisfireSound());
                return false;
            }

            speaker.Play(firearm.ShotSound());
            Shot(position, pitch, yaw, firearm);
            return true;
        }

        public bool TryToReload()
        {
            if (!(inventory.Equipped() is Firearm firearm))
            {
                return false;
            }

            if (firearm.MagazineFull() || inventory.Amount(firearm.AmmoType()) == 0)
            {
                return false;
            }

            if (!hands.TryTake(HandsAction.Reloading, firearm.ReloadTime(), true, () => Reloaded(firearm)))
            {
                return false;
            }

            speaker.Play(firearm.ReloadSound());
            Log.Info("Reload of {} started, {}s", firearm.FirearmType(), firearm.ReloadTime());
            return true;
        }

        private void Reloaded(Firearm firearm)
        {
            StackableItem ammoType = firearm.AmmoType();
            int spent = firearm.Reload(inventory.Amount(ammoType));
            inventory.Remove(ammoType, spent, InventoryOnConflictAction.Partly);
            Log.Info("Reloaded {} with {} rounds, {} {} left", firearm.FirearmType(), spent, inventory.Amount(ammoType), ammoType);
        }

        private void Shot(Vector3 position, float pitch, float yaw, Firearm firearm)
        {
            Ray look = Sight.LookRay(position, pitch, yaw);

            if (!sight.Cast(look, firearm.Distance(), out RaycastHit hit))
            {
                Log.Info("Shot from {} missed", position);
                return;
            }

            if (!EntityBody.TryResolve(hit.collider, out Guid targetId))
            {
                Log.Info("Shot from {} hit map at {}", position, hit.point);
                return;
            }

            Health.Health health = worldEntities.ById(targetId)?.Get<Health.Health>();
            if (health == null)
            {
                Log.Info("Shot from {} hit entity {} without health", position, targetId);
                return;
            }

            health.Damage(firearm.Damage());
            Log.Info("Shot from {} hit entity {} for {} damage", position, targetId, firearm.Damage());
        }
    }
}
