using UnityEngine;
using Shooter.Client.Worlds.Entities;
using Shooter.Server.Worlds;

namespace Shooter.Client.Aiming
{
    public class Aim
    {
        private const float Range = 1000f;

        private readonly Sight sight = new Sight(Physics.defaultPhysicsScene);

        public RaycastHit? Target { get; private set; }

        public void At(Vector3 position, float pitch, float yaw)
        {
            Ray look = Sight.LookRay(position, pitch, yaw);
            Target = sight.TryCast(look, Range, out RaycastHit hit) ? hit : (RaycastHit?)null;
        }

        public EntityView TargetView(float reach)
        {
            if (Target == null || Target.Value.distance > reach) return null;

            return ClientEntityBody.Resolve(Target.Value.collider);
        }
    }
}
