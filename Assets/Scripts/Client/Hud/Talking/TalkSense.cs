using Shooter.Client.Aiming;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities.Parts.Talker;
using UnityEngine;
using Shooter.Server.Worlds.Entities;

namespace Shooter.Client.Hud.Talking
{
    public class TalkSense
    {
        private readonly ClientWorld world;
        private readonly Aim aim;

        public TalkSense(ClientWorld world, Aim aim)
        {
            this.world = world;
            this.aim = aim;
        }

        public bool TalkerTargeted()
        {
            RaycastHit? target = aim.Target;
            if (target == null)
            {
                return false;
            }

            if (target.Value.distance >= Talker.TalkReach)
            {
                return false;
            }

            if (!target.Value.collider.TryGetComponent(out EntityBody entityBody))
            {
                return false;
            }

            EntityState entity = world.Entities[entityBody.Id];
            if (entity == null)
            {
                return false;
            }

            TalkerState talker = entity.Part<TalkerState>();
            if (talker == null)
            {
                return false;
            }

            return true;
        }
    }
}
