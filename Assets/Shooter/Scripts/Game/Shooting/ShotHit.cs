using UnityEngine;

namespace Shooter.Game
{
    public readonly struct ShotHit
    {
        public ShotHit(Vector3 origin, Vector3 direction) : this(origin, direction, null, default, default, null, null, default)
        {
        }

        public ShotHit(Vector3 origin, Vector3 direction, Collider collider, Vector3 point, Vector3 normal, PhysicsMaterial surface, Health victim, BodyPart part)
        {
            Origin = origin;
            Direction = direction;
            Collider = collider;
            Point = point;
            Normal = normal;
            Surface = surface;
            Victim = victim;
            Part = part;
        }

        public Vector3 Origin { get; }

        public Vector3 Direction { get; }

        public Collider Collider { get; }

        public Vector3 Point { get; }

        public Vector3 Normal { get; }

        public PhysicsMaterial Surface { get; }

        public Health Victim { get; }

        public BodyPart Part { get; }

        public bool IsMiss => Collider == null;
    }
}
