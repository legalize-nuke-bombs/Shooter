using UnityEngine;

namespace Shooter.Server.Worlds
{
    public sealed class Sight
    {
        public const float EyeHeight = 0.75f;

        private readonly PhysicsScene physics;

        public Sight(PhysicsScene physics)
        {
            this.physics = physics;
        }

        public static Ray LookRay(Vector3 position, float pitch, float yaw)
        {
            Vector3 eyes = position + Vector3.up * EyeHeight;
            return new Ray(eyes, Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward);
        }

        public bool Cast(Ray look, float reach, out RaycastHit hit)
        {
            return physics.Raycast(look.origin, look.direction, out hit, reach);
        }
    }
}
