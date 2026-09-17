using UnityEngine;

namespace Shooter.Game.Core
{
    [RequireComponent(typeof(TextMarker))]
    public class CapsuleMarker : MonoBehaviour
    {
        private const float Height = 2f;
        private const float Ring = 0.4f;

        private static readonly Color DefaultTint = new(0.35f, 0.9f, 1f);

        private void OnDrawGizmos()
        {
            Draw(transform.position, DefaultTint);
        }

        public static void Draw(Vector3 feet, Color tint)
        {
            Vector3 bottom = feet + Vector3.up * Ring;
            Vector3 top = feet + Vector3.up * (Height - Ring);

            Gizmos.color = tint;
            Gizmos.DrawWireSphere(bottom, Ring);
            Gizmos.DrawWireSphere(top, Ring);
            Gizmos.DrawLine(bottom + Vector3.right * Ring, top + Vector3.right * Ring);
            Gizmos.DrawLine(bottom + Vector3.left * Ring, top + Vector3.left * Ring);
            Gizmos.DrawLine(bottom + Vector3.forward * Ring, top + Vector3.forward * Ring);
            Gizmos.DrawLine(bottom + Vector3.back * Ring, top + Vector3.back * Ring);
        }
    }
}
