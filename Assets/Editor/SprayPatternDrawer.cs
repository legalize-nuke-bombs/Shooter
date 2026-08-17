using Shooter.Game.Combat;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    [CustomPropertyDrawer(typeof(SprayPattern))]
    public class SprayPatternDrawer : PropertyDrawer
    {
        private const string RangeKey = "Shooter.SprayPattern.Range";
        private const string PanXKey = "Shooter.SprayPattern.PanX";
        private const string PanYKey = "Shooter.SprayPattern.PanY";
        private const float CanvasSide = 280f;
        private const float GrabRadius = 12f;

        private int dragged = -1;
        private bool panning;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty points = property.FindPropertyRelative("points");

            return EditorGUIUtility.singleLineHeight + 4f + CanvasSide + 4f + EditorGUI.GetPropertyHeight(points, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty points = property.FindPropertyRelative("points");

            var header = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(header, label.text,
                $"{points.arraySize} bullets | LMB add/drag, Alt+LMB delete, RMB pan, wheel zoom");

            float side = Mathf.Min(CanvasSide, position.width - 4f);
            var canvas = new Rect(position.x + (position.width - side) * 0.5f, header.yMax + 2f, side, side);
            float range = EditorPrefs.GetFloat(RangeKey, 5f);
            var pan = new Vector2(EditorPrefs.GetFloat(PanXKey, 0f), EditorPrefs.GetFloat(PanYKey, 0f));

            Mouse(canvas, points, range, pan);

            var list = new Rect(position.x, canvas.yMax + 4f, position.width,
                EditorGUI.GetPropertyHeight(points, true));
            EditorGUI.PropertyField(list, points, new GUIContent("Points"), true);

            if (Event.current.type != EventType.Repaint) return;

            EditorGUI.DrawRect(canvas, new Color(0.12f, 0.12f, 0.12f));

            GUI.BeginClip(canvas);
            var local = new Rect(0f, 0f, canvas.width, canvas.height);
            Grid(local, range, pan);
            Pattern(local, points, range, pan);
            GUI.EndClip();
        }

        private void Mouse(Rect canvas, SerializedProperty points, float range, Vector2 pan)
        {
            Event current = Event.current;

            if (current.type == EventType.MouseUp)
            {
                if (current.button == 0 && dragged >= 0)
                {
                    dragged = -1;
                    current.Use();
                }
                else if (current.button == 1 && panning)
                {
                    panning = false;
                    current.Use();
                }

                return;
            }

            bool inside = canvas.Contains(current.mousePosition);

            switch (current.type)
            {
                case EventType.ScrollWheel when inside:
                    Vector2 anchor = Degrees(canvas, current.mousePosition, range, pan);
                    float zoomed = Mathf.Clamp(range * Mathf.Pow(1.1f, Mathf.Sign(current.delta.y)), 1f, 45f);
                    Vector2 kept = anchor - (Degrees(canvas, current.mousePosition, zoomed, pan) - pan);

                    EditorPrefs.SetFloat(RangeKey, zoomed);
                    EditorPrefs.SetFloat(PanXKey, kept.x);
                    EditorPrefs.SetFloat(PanYKey, kept.y);
                    current.Use();
                    break;

                case EventType.MouseDown when current.button == 0 && current.alt && inside:
                    int victim = Closest(canvas, points, current.mousePosition, range, pan);

                    if (victim >= 0)
                    {
                        points.DeleteArrayElementAtIndex(victim);
                        current.Use();
                    }

                    break;

                case EventType.MouseDown when current.button == 0 && inside:
                    dragged = Closest(canvas, points, current.mousePosition, range, pan);

                    if (dragged < 0)
                    {
                        points.arraySize++;
                        dragged = points.arraySize - 1;
                        points.GetArrayElementAtIndex(dragged).vector2Value =
                            Degrees(canvas, current.mousePosition, range, pan);
                    }

                    current.Use();
                    break;

                case EventType.MouseDrag when current.button == 0 && dragged >= 0:
                    points.GetArrayElementAtIndex(dragged).vector2Value =
                        Degrees(canvas, current.mousePosition, range, pan);
                    current.Use();
                    break;

                case EventType.MouseDown when current.button == 1 && inside:
                    panning = true;
                    current.Use();
                    break;

                case EventType.MouseDrag when current.button == 1 && panning:
                    float half = canvas.width * 0.5f;
                    EditorPrefs.SetFloat(PanXKey, pan.x - current.delta.x * range / half);
                    EditorPrefs.SetFloat(PanYKey, pan.y + current.delta.y * range / half);
                    current.Use();
                    break;
            }
        }

        private static int Closest(Rect canvas, SerializedProperty points, Vector2 mouse, float range, Vector2 pan)
        {
            int closest = -1;
            float nearest = GrabRadius;

            for (int i = 0; i < points.arraySize; i++)
            {
                Vector2 pixel = Pixel(canvas, points.GetArrayElementAtIndex(i).vector2Value, range, pan);
                float distance = Vector2.Distance(pixel, mouse);

                if (distance >= nearest) continue;

                closest = i;
                nearest = distance;
            }

            return closest;
        }

        private static Vector2 Pixel(Rect canvas, Vector2 degrees, float range, Vector2 pan)
        {
            float half = canvas.width * 0.5f;

            return new Vector2(
                canvas.center.x + (degrees.x - pan.x) / range * half,
                canvas.center.y - (degrees.y - pan.y) / range * half);
        }

        private static Vector2 Degrees(Rect canvas, Vector2 pixel, float range, Vector2 pan)
        {
            float half = canvas.width * 0.5f;

            return new Vector2(
                pan.x + (pixel.x - canvas.center.x) / half * range,
                pan.y + (canvas.center.y - pixel.y) / half * range);
        }

        private static void Grid(Rect canvas, float range, Vector2 pan)
        {
            float step = range <= 6f ? 1f : range <= 14f ? 2f : 5f;
            var faint = new Color(1f, 1f, 1f, 0.05f);
            var axis = new Color(1f, 1f, 1f, 0.18f);
            float half = canvas.width * 0.5f;

            for (int i = Mathf.CeilToInt((pan.x - range) / step); i * step <= pan.x + range; i++)
            {
                float x = canvas.center.x + (i * step - pan.x) / range * half;
                EditorGUI.DrawRect(new Rect(x, canvas.y, 1f, canvas.height), i == 0 ? axis : faint);
            }

            for (int i = Mathf.CeilToInt((pan.y - range) / step); i * step <= pan.y + range; i++)
            {
                float y = canvas.center.y - (i * step - pan.y) / range * half;
                EditorGUI.DrawRect(new Rect(canvas.x, y, canvas.width, 1f), i == 0 ? axis : faint);
            }

            float spread = Mathf.Tan(step * Mathf.Deg2Rad) * 10f * 100f;
            GUI.Label(
                new Rect(canvas.x + 4f, canvas.yMax - 18f, canvas.width - 8f, 16f),
                $"cell {step:0.#}° ≈ {spread:0} cm at 10 m",
                EditorStyles.miniLabel);
        }

        private void Pattern(Rect canvas, SerializedProperty points, float range, Vector2 pan)
        {
            int count = points.arraySize;
            if (count == 0) return;

            var line = new Vector3[count + 1];
            line[0] = Pixel(canvas, Vector2.zero, range, pan);

            for (int i = 0; i < count; i++)
                line[i + 1] = Pixel(canvas, points.GetArrayElementAtIndex(i).vector2Value, range, pan);

            Handles.color = new Color(1f, 0.55f, 0.1f, 0.8f);
            Handles.DrawAAPolyLine(2.5f, line);

            for (int i = 0; i < count; i++)
            {
                Vector2 degrees = points.GetArrayElementAtIndex(i).vector2Value;
                var pixel = (Vector2)line[i + 1];

                Handles.color = i == dragged ? Color.white :
                    i == 0 ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.55f, 0.1f);
                Handles.DrawSolidDisc(pixel, Vector3.forward, 4.5f);

                GUI.Label(
                    new Rect(pixel.x + 6f, pixel.y - 7f, 84f, 14f),
                    $"{degrees.x:0.#}, {degrees.y:0.#}",
                    EditorStyles.miniLabel);
            }
        }
    }
}
