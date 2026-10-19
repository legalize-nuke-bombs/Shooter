using UnityEngine;

namespace Shooter.Infrastructure
{
    [RequireComponent(typeof(TextMarker))]
    public class CapsuleMarker : ShooterBehaviour
    {
        private const float Height = 2f;
        private const float Radius = 0.4f;

        private static readonly Color Tint = new(0.35f, 0.9f, 1f);

        [SerializeField] private Vector3 offset;

        private void OnDrawGizmos()
        {
            Vector3 feet = transform.position + offset;
            Vector3 bottom = feet + Vector3.up * Radius;
            Vector3 top = feet + Vector3.up * (Height - Radius);

            Gizmos.color = Tint;
            Gizmos.DrawWireSphere(bottom, Radius);
            Gizmos.DrawWireSphere(top, Radius);
            Gizmos.DrawLine(bottom + Vector3.right * Radius, top + Vector3.right * Radius);
            Gizmos.DrawLine(bottom + Vector3.left * Radius, top + Vector3.left * Radius);
            Gizmos.DrawLine(bottom + Vector3.forward * Radius, top + Vector3.forward * Radius);
            Gizmos.DrawLine(bottom + Vector3.back * Radius, top + Vector3.back * Radius);
        }
    }
}
