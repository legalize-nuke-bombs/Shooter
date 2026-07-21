using UnityEngine;
using Shooter.Server.Worlds;

namespace Shooter.Client.Aiming
{
    public class Aim
    {
        private const float Range = 1000f;

        public RaycastHit? Target { get; private set; }

        private readonly Sight sight = new Sight(Physics.defaultPhysicsScene);

        public void Tick(Vector3 position, float pitch, float yaw)
        {
            Ray look = Sight.LookRay(position, pitch, yaw);
            Target = sight.Cast(look, Range, out RaycastHit hit)
                ? hit
                : (RaycastHit?)null;
        }
    }
}
