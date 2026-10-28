using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextGizmo))]
    public class CapsuleGizmo : MonoBehaviour
    {
        private const float Height = 2f;
        private const float Radius = 0.4f;

        private static readonly Color Tint = new(0.35f, 0.9f, 1f);

        [SerializeField] private Vector3 offset;

        private void OnDrawGizmos()
        {
            Vector3 feetPosition = transform.position + offset;
            Vector3 bottomSphereCenter = feetPosition + Vector3.up * Radius;
            Vector3 topSphereCenter = feetPosition + Vector3.up * (Height - Radius);

            Gizmos.color = Tint;
            Gizmos.DrawWireSphere(bottomSphereCenter, Radius);
            Gizmos.DrawWireSphere(topSphereCenter, Radius);
            Gizmos.DrawLine(bottomSphereCenter + Vector3.right * Radius, topSphereCenter + Vector3.right * Radius);
            Gizmos.DrawLine(bottomSphereCenter + Vector3.left * Radius, topSphereCenter + Vector3.left * Radius);
            Gizmos.DrawLine(bottomSphereCenter + Vector3.forward * Radius, topSphereCenter + Vector3.forward * Radius);
            Gizmos.DrawLine(bottomSphereCenter + Vector3.back * Radius, topSphereCenter + Vector3.back * Radius);
        }
    }
}
