using Shooter.Game.Combat;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    [CustomPropertyDrawer(typeof(SprayPattern))]
    public class SprayPatternDrawer : PropertyDrawer
    {
        private const string RangeKey = "Shooter.SprayPattern.Range";
        private const float CanvasSide = 280f;
        private const float GrabRadius = 12f;

        private int dragged = -1;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + 4f + CanvasSide;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty points = property.FindPropertyRelative("points");

            var header = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(header, label.text, $"{points.arraySize} bullets | LMB add/drag, RMB delete, wheel zoom");

            float side = Mathf.Min(CanvasSide, position.width - 4f);
            var canvas = new Rect(position.x + (position.width - side) * 0.5f, header.yMax + 2f, side, side);
            float range = EditorPrefs.GetFloat(RangeKey, 5f);

            Mouse(canvas, points, range);

            if (Event.current.type != EventType.Repaint) return;

            EditorGUI.DrawRect(canvas, new Color(0.12f, 0.12f, 0.12f));

            GUI.BeginClip(canvas);
            var local = new Rect(0f, 0f, canvas.width, canvas.height);
            Grid(local, range);
            Pattern(local, points, range);
            GUI.EndClip();
        }

        private void Mouse(Rect canvas, SerializedProperty points, float range)
        {
            Event current = Event.current;

            if (current.type == EventType.MouseUp && dragged >= 0)
            {
                dragged = -1;
                current.Use();
                return;
            }

            bool inside = canvas.Contains(current.mousePosition);

            switch (current.type)
            {
                case EventType.ScrollWheel when inside:
                    float zoomed = range * Mathf.Pow(1.1f, Mathf.Sign(current.delta.y));
                    EditorPrefs.SetFloat(RangeKey, Mathf.Clamp(zoomed, 1f, 45f));
                    current.Use();
                    break;

                case EventType.MouseDown when current.button == 0 && inside:
                    dragged = Closest(canvas, points, current.mousePosition, range);

                    if (dragged < 0)
                    {
                        points.arraySize++;
                        dragged = points.arraySize - 1;
                        points.GetArrayElementAtIndex(dragged).vector2Value = Degrees(canvas, current.mousePosition, range);
                    }

                    current.Use();
                    break;

                case EventType.MouseDrag when current.button == 0 && dragged >= 0:
                    points.GetArrayElementAtIndex(dragged).vector2Value = Degrees(canvas, current.mousePosition, range);
                    current.Use();
                    break;

                case EventType.MouseDown when current.button == 1 && inside:
                    int victim = Closest(canvas, points, current.mousePosition, range);

                    if (victim >= 0)
                    {
                        points.DeleteArrayElementAtIndex(victim);
                        current.Use();
                    }

                    break;
            }
        }

        private static int Closest(Rect canvas, SerializedProperty points, Vector2 mouse, float range)
        {
            int closest = -1;
            float nearest = GrabRadius;

            for (int i = 0; i < points.arraySize; i++)
            {
                Vector2 pixel = Pixel(canvas, points.GetArrayElementAtIndex(i).vector2Value, range);
                float distance = Vector2.Distance(pixel, mouse);

                if (distance >= nearest) continue;

                closest = i;
                nearest = distance;
            }

            return closest;
        }

        private static Vector2 Pixel(Rect canvas, Vector2 degrees, float range)
        {
            float half = canvas.width * 0.5f;

            return new Vector2(
                canvas.center.x + degrees.x / range * half,
                canvas.center.y - degrees.y / range * half);
        }

        private static Vector2 Degrees(Rect canvas, Vector2 pixel, float range)
        {
            float half = canvas.width * 0.5f;

            return new Vector2(
                (pixel.x - canvas.center.x) / half * range,
                (canvas.center.y - pixel.y) / half * range);
        }

        private static void Grid(Rect canvas, float range)
        {
            float step = range <= 6f ? 1f : range <= 14f ? 2f : 5f;
            var faint = new Color(1f, 1f, 1f, 0.05f);
            var mark = EditorStyles.miniLabel;
            float half = canvas.width * 0.5f;

            for (float degree = step; degree < range; degree += step)
            {
                float offset = degree / range * half;

                EditorGUI.DrawRect(new Rect(canvas.center.x + offset, canvas.y, 1f, canvas.height), faint);
                EditorGUI.DrawRect(new Rect(canvas.center.x - offset, canvas.y, 1f, canvas.height), faint);
                EditorGUI.DrawRect(new Rect(canvas.x, canvas.center.y + offset, canvas.width, 1f), faint);
                EditorGUI.DrawRect(new Rect(canvas.x, canvas.center.y - offset, canvas.width, 1f), faint);

                GUI.Label(new Rect(canvas.center.x + offset - 12f, canvas.center.y + 1f, 24f, 13f), $"{degree:0.#}", mark);
                GUI.Label(new Rect(canvas.center.x - offset - 12f, canvas.center.y + 1f, 24f, 13f), $"-{degree:0.#}", mark);
                GUI.Label(new Rect(canvas.center.x + 3f, canvas.center.y - offset - 1f, 30f, 13f), $"{degree:0.#}", mark);
                GUI.Label(new Rect(canvas.center.x + 3f, canvas.center.y + offset - 12f, 30f, 13f), $"-{degree:0.#}", mark);
            }

            var axis = new Color(1f, 1f, 1f, 0.15f);
            EditorGUI.DrawRect(new Rect(canvas.center.x, canvas.y, 1f, canvas.height), axis);
            EditorGUI.DrawRect(new Rect(canvas.x, canvas.center.y, canvas.width, 1f), axis);

            float spread = Mathf.Tan(step * Mathf.Deg2Rad) * 10f * 100f;
            GUI.Label(
                new Rect(canvas.x + 4f, canvas.yMax - 18f, canvas.width - 8f, 16f),
                $"{step:0.#}° ≈ {spread:0} cm at 10 m",
                mark);

            GUI.Label(
                new Rect(canvas.xMax - 44f, canvas.y + 2f, 42f, 16f),
                $"±{range:0.#}°",
                mark);
        }

        private void Pattern(Rect canvas, SerializedProperty points, float range)
        {
            int count = points.arraySize;
            if (count == 0) return;

            var line = new Vector3[count + 1];
            line[0] = Pixel(canvas, Vector2.zero, range);

            for (int i = 0; i < count; i++)
                line[i + 1] = Pixel(canvas, points.GetArrayElementAtIndex(i).vector2Value, range);

            Handles.color = new Color(1f, 0.55f, 0.1f, 0.8f);
            Handles.DrawAAPolyLine(2.5f, line);

            for (int i = 0; i < count; i++)
            {
                var pixel = (Vector2)line[i + 1];

                Handles.color = i == dragged ? Color.white : i == 0 ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.55f, 0.1f);
                Handles.DrawSolidDisc(pixel, Vector3.forward, 4.5f);

                GUI.Label(new Rect(pixel.x + 5f, pixel.y - 16f, 28f, 14f), (i + 1).ToString(), EditorStyles.miniLabel);
            }
        }
    }
}
