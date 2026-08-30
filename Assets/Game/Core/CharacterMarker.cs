using UnityEngine;

namespace Shooter.Game.Core
{
    public class CharacterMarker : MonoBehaviour
    {
        private const float Height = 2f;
        private const float Ring = 0.4f;
        private const float LabelLift = 0.2f;

        private static readonly Color DefaultTint = new(0.35f, 0.9f, 1f);

        [SerializeField] private string label;
        [SerializeField] private Color tint = DefaultTint;

        private void OnDrawGizmos()
        {
            Draw(transform.position, string.IsNullOrEmpty(label) ? gameObject.name : label, tint);
        }

        public static void Draw(Vector3 feet, string label)
        {
            Draw(feet, label, DefaultTint);
        }

        public static void Draw(Vector3 feet, string label, Color tint)
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

#if UNITY_EDITOR
            UnityEditor.Handles.Label(feet + Vector3.up * (Height + LabelLift), label);
#endif
        }
    }
}
