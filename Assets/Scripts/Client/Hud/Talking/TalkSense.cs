using Shooter.Client.Aiming;
using Shooter.Client.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Talker;
using UnityEngine;
using Shooter.Server.Worlds.Entities.Parts.Health;

namespace Shooter.Client.Hud.Talking
{
    public class TalkSense
    {
        private readonly Aim aim;

        public TalkSense(Aim aim)
        {
            this.aim = aim;
        }

        public EntityView TargetTalker()
        {
            RaycastHit? target = aim.Target;
            if (target == null)
            {
                return null;
            }

            if (target.Value.distance >= Talker.TalkReach)
            {
                return null;
            }

            EntityBody bridge = target.Value.collider.GetComponentInParent<EntityBody>();
            if (bridge == null)
            {
                return null;
            }

            if (bridge.View.State.Part<TalkerState>() == null)
            {
                return null;
            }

            HealthState health = bridge.View.State.Part<HealthState>();
            if (health != null && health.Hp == 0)
            {
                return null;
            }

            return bridge.View;
        }

        public bool TalkerTargeted()
        {
            return TargetTalker() != null;
        }
    }
}
