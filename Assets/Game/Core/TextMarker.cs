using UnityEngine;

namespace Shooter.Game.Core
{
    public class TextMarker : MonoBehaviour
    {
        private static readonly Color DefaultTint = new(1f, 1f, 1f);

        [SerializeField] private string label;
        [SerializeField] private Color textColor = DefaultTint;
        [SerializeField] private Vector3 offset = new Vector3(0, 1.2f, 0);

        private void OnDrawGizmos()
        {
            string textToDraw = string.IsNullOrEmpty(label) ? gameObject.name : label;
            Draw(transform.position + offset, textToDraw, textColor);
        }

        public static void Draw(Vector3 position, string label)
        {
            Draw(position, label, DefaultTint);
        }

        public static void Draw(Vector3 position, string label, Color tint)
        {
#if UNITY_EDITOR
            var style = new GUIStyle();
            style.normal.textColor = tint;
            style.alignment = TextAnchor.MiddleCenter;

            UnityEditor.Handles.Label(position, label, style);
#endif
        }
    }
}
