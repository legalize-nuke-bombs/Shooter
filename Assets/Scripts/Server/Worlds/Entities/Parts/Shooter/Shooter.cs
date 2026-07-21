using System;
using Shooter.Logging;
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

        private float cooldown;

        public Shooter(Inventory.Inventory inventory, Speaker.Speaker speaker, Sight sight, WorldEntities worldEntities)
        {
            this.inventory = inventory;
            this.speaker = speaker;
            this.sight = sight;
            this.worldEntities = worldEntities;
        }

        public override void Tick(Entity self, float dt)
        {
            cooldown = Mathf.Max(0f, cooldown - dt);
        }

        public bool TryToShoot(Vector3 position, float pitch, float yaw)
        {
            if (cooldown > 0f)
            {
                return false;
            }

            if (!(inventory.Equipped() is Firearm firearm))
            {
                return false;
            }

            cooldown = firearm.FireInterval();

            if (!firearm.TryToShoot())
            {
                speaker.Play(firearm.MisfireSound());
                return false;
            }

            speaker.Play(firearm.ShotSound());
            Shot(position, pitch, yaw, firearm);
            return true;
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
