using UnityEngine;

namespace Shooter.Game
{
    public readonly struct ShotHit
    {
        public ShotHit(Collider collider, Vector3 point, Vector3 normal, Health victim, BodyPart part)
        {
            Collider = collider;
            Point = point;
            Normal = normal;
            Victim = victim;
            Part = part;
        }

        public Collider Collider { get; }

        public Vector3 Point { get; }

        public Vector3 Normal { get; }

        public Health Victim { get; }

        public BodyPart Part { get; }

        public bool IsMiss => Collider == null;
    }
}
