using UnityEngine;

namespace Shooter.Infrastructure
{
    public class TextGizmo : ShooterBehaviour
    {
        [SerializeField] private string label;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Vector3 offset = new(0f, 1.2f, 0f);

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            string text = string.IsNullOrEmpty(label) ? gameObject.name : label;
            GUIStyle style = new() { alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = textColor;

            UnityEditor.Handles.Label(transform.position + offset, text, style);
#endif
        }
    }
}
