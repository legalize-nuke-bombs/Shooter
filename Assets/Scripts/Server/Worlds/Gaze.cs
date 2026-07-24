using UnityEngine;
using Shooter.Server.Worlds.Entities;

namespace Shooter.Server.Worlds
{
    public sealed class Gaze
    {
        private readonly Sight sight;
        private readonly WorldEntities entities;

        public Gaze(Sight sight, WorldEntities entities)
        {
            this.sight = sight;
            this.entities = entities;
        }

        public bool TryLook(Vector3 from, float pitch, float yaw, float reach, out RaycastHit hit)
        {
            return sight.TryCast(Sight.LookRay(from, pitch, yaw), reach, out hit);
        }

        public bool TryLookAt(Vector3 from, float pitch, float yaw, float reach, out Entity entity)
        {
            entity = null;
            if (!TryLook(from, pitch, yaw, reach, out RaycastHit hit)) return false;

            entity = Resolve(hit);
            return entity != null;
        }

        public Entity Resolve(RaycastHit hit)
        {
            if (hit.collider == null) return null;
            if (!ServerEntityBody.TryResolve(hit.collider, out System.Guid id)) return null;

            return entities.ById(id);
        }
    }
}
