using Shooter.Game;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    [CustomPropertyDrawer(typeof(SprayPattern))]
    public class SprayPatternDrawer : PropertyDrawer
    {
        private const string RangeKey = "Shooter.SprayPattern.Range";
        private const string PanXKey = "Shooter.SprayPattern.PanX";
        private const string PanYKey = "Shooter.SprayPattern.PanY";
        private const float CanvasSide = 280f;
        private const float GrabRadius = 12f;

        private int draggedPointIndex = -1;
        private bool panning;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty pointsProperty = property.FindPropertyRelative(SprayPattern.PointsField);

            return EditorGUIUtility.singleLineHeight + 4f + CanvasSide + 4f + EditorGUI.GetPropertyHeight(pointsProperty, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty pointsProperty = property.FindPropertyRelative(SprayPattern.PointsField);

            Rect header = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(header, label.text, $"{pointsProperty.arraySize} bullets | LMB add/drag, Alt+LMB delete, RMB pan, wheel zoom");

            float side = Mathf.Min(CanvasSide, position.width - 4f);
            Rect canvas = new(position.x + (position.width - side) * 0.5f, header.yMax + 2f, side, side);
            float range = EditorPrefs.GetFloat(RangeKey, 5f);
            Vector2 pan = new(EditorPrefs.GetFloat(PanXKey, 0f), EditorPrefs.GetFloat(PanYKey, 0f));

            HandleMouse(canvas, pointsProperty, range, pan);

            Rect pointsListRect = new(position.x, canvas.yMax + 4f, position.width, EditorGUI.GetPropertyHeight(pointsProperty, true));
            EditorGUI.PropertyField(pointsListRect, pointsProperty, new GUIContent("Points"), true);

            if (Event.current.type != EventType.Repaint) return;

            EditorGUI.DrawRect(canvas, new Color(0.12f, 0.12f, 0.12f));

            GUI.BeginClip(canvas);
            Rect clippedCanvas = new(0f, 0f, canvas.width, canvas.height);
            DrawGrid(clippedCanvas, range, pan);
            DrawPattern(clippedCanvas, pointsProperty, range, pan);
            GUI.EndClip();
        }

        private void HandleMouse(Rect canvas, SerializedProperty pointsProperty, float range, Vector2 pan)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseUp)
            {
                if (currentEvent.button == 0 && draggedPointIndex >= 0)
                {
                    draggedPointIndex = -1;
                    currentEvent.Use();
                }
                else if (currentEvent.button == 1 && panning)
                {
                    panning = false;
                    currentEvent.Use();
                }

                return;
            }

            bool isMouseOnCanvas = canvas.Contains(currentEvent.mousePosition);

            switch (currentEvent.type)
            {
                case EventType.ScrollWheel when isMouseOnCanvas:
                    Vector2 degreesUnderMouse = ConvertPixelToDegrees(canvas, currentEvent.mousePosition, range, pan);
                    float zoomedRange = Mathf.Clamp(range * Mathf.Pow(1.1f, Mathf.Sign(currentEvent.delta.y)), 1f, 45f);
                    Vector2 panKeepingMouse = degreesUnderMouse - (ConvertPixelToDegrees(canvas, currentEvent.mousePosition, zoomedRange, pan) - pan);

                    EditorPrefs.SetFloat(RangeKey, zoomedRange);
                    EditorPrefs.SetFloat(PanXKey, panKeepingMouse.x);
                    EditorPrefs.SetFloat(PanYKey, panKeepingMouse.y);
                    currentEvent.Use();
                    break;

                case EventType.MouseDown when currentEvent.button == 0 && currentEvent.alt && isMouseOnCanvas:
                    int pointIndexToDelete = FindPointIndexUnderMouse(canvas, pointsProperty, currentEvent.mousePosition, range, pan);

                    if (pointIndexToDelete >= 0)
                    {
                        pointsProperty.DeleteArrayElementAtIndex(pointIndexToDelete);
                        currentEvent.Use();
                    }

                    break;

                case EventType.MouseDown when currentEvent.button == 0 && isMouseOnCanvas:
                    draggedPointIndex = FindPointIndexUnderMouse(canvas, pointsProperty, currentEvent.mousePosition, range, pan);

                    if (draggedPointIndex < 0)
                    {
                        pointsProperty.arraySize++;
                        draggedPointIndex = pointsProperty.arraySize - 1;
                        pointsProperty.GetArrayElementAtIndex(draggedPointIndex).vector2Value = ConvertPixelToDegrees(canvas, currentEvent.mousePosition, range, pan);
                    }

                    currentEvent.Use();
                    break;

                case EventType.MouseDrag when currentEvent.button == 0 && draggedPointIndex >= 0:
                    pointsProperty.GetArrayElementAtIndex(draggedPointIndex).vector2Value = ConvertPixelToDegrees(canvas, currentEvent.mousePosition, range, pan);
                    currentEvent.Use();
                    break;

                case EventType.MouseDown when currentEvent.button == 1 && isMouseOnCanvas:
                    panning = true;
                    currentEvent.Use();
                    break;

                case EventType.MouseDrag when currentEvent.button == 1 && panning:
                    float halfWidth = canvas.width * 0.5f;
                    EditorPrefs.SetFloat(PanXKey, pan.x - currentEvent.delta.x * range / halfWidth);
                    EditorPrefs.SetFloat(PanYKey, pan.y + currentEvent.delta.y * range / halfWidth);
                    currentEvent.Use();
                    break;
            }
        }

        private static int FindPointIndexUnderMouse(Rect canvas, SerializedProperty pointsProperty, Vector2 mousePosition, float range, Vector2 pan)
        {
            int closestIndex = -1;
            float closestDistance = GrabRadius;

            for (int i = 0; i < pointsProperty.arraySize; i++)
            {
                Vector2 pixel = ConvertDegreesToPixel(canvas, pointsProperty.GetArrayElementAtIndex(i).vector2Value, range, pan);
                float distance = Vector2.Distance(pixel, mousePosition);

                if (distance >= closestDistance) continue;

                closestIndex = i;
                closestDistance = distance;
            }

            return closestIndex;
        }

        private static Vector2 ConvertDegreesToPixel(Rect canvas, Vector2 degrees, float range, Vector2 pan)
        {
            float halfWidth = canvas.width * 0.5f;

            return new Vector2(canvas.center.x + (degrees.x - pan.x) / range * halfWidth, canvas.center.y - (degrees.y - pan.y) / range * halfWidth);
        }

        private static Vector2 ConvertPixelToDegrees(Rect canvas, Vector2 pixel, float range, Vector2 pan)
        {
            float halfWidth = canvas.width * 0.5f;

            return new Vector2(pan.x + (pixel.x - canvas.center.x) / halfWidth * range, pan.y + (canvas.center.y - pixel.y) / halfWidth * range);
        }

        private static void DrawGrid(Rect canvas, float range, Vector2 pan)
        {
            float gridStep = range <= 6f ? 1f : range <= 14f ? 2f : 5f;
            Color gridLineColor = new(1f, 1f, 1f, 0.05f);
            Color axisLineColor = new(1f, 1f, 1f, 0.18f);
            float halfWidth = canvas.width * 0.5f;

            for (int i = Mathf.CeilToInt((pan.x - range) / gridStep); i * gridStep <= pan.x + range; i++)
            {
                float x = canvas.center.x + (i * gridStep - pan.x) / range * halfWidth;
                EditorGUI.DrawRect(new Rect(x, canvas.y, 1f, canvas.height), i == 0 ? axisLineColor : gridLineColor);
            }

            for (int i = Mathf.CeilToInt((pan.y - range) / gridStep); i * gridStep <= pan.y + range; i++)
            {
                float y = canvas.center.y - (i * gridStep - pan.y) / range * halfWidth;
                EditorGUI.DrawRect(new Rect(canvas.x, y, canvas.width, 1f), i == 0 ? axisLineColor : gridLineColor);
            }

            float cellSpreadAtTenMeters = Mathf.Tan(gridStep * Mathf.Deg2Rad) * 10f * 100f;
            GUI.Label(new Rect(canvas.x + 4f, canvas.yMax - 18f, canvas.width - 8f, 16f), $"cell {gridStep:0.#}° ≈ {cellSpreadAtTenMeters:0} cm at 10 m", EditorStyles.miniLabel);
        }

        private void DrawPattern(Rect canvas, SerializedProperty pointsProperty, float range, Vector2 pan)
        {
            int pointCount = pointsProperty.arraySize;
            if (pointCount == 0) return;

            Vector3[] linePixels = new Vector3[pointCount + 1];
            linePixels[0] = ConvertDegreesToPixel(canvas, Vector2.zero, range, pan);

            for (int i = 0; i < pointCount; i++)
            {
                linePixels[i + 1] = ConvertDegreesToPixel(canvas, pointsProperty.GetArrayElementAtIndex(i).vector2Value, range, pan);
            }

            Handles.color = new Color(1f, 0.55f, 0.1f, 0.8f);
            Handles.DrawAAPolyLine(2.5f, linePixels);

            for (int i = 0; i < pointCount; i++)
            {
                Vector2 degrees = pointsProperty.GetArrayElementAtIndex(i).vector2Value;
                Vector2 pixel = linePixels[i + 1];

                Handles.color = i == draggedPointIndex ? Color.white : i == 0 ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.55f, 0.1f);
                Handles.DrawSolidDisc(pixel, Vector3.forward, 4.5f);

                GUI.Label(new Rect(pixel.x + 6f, pixel.y - 7f, 84f, 14f), $"{degrees.x:0.#}, {degrees.y:0.#}", EditorStyles.miniLabel);
            }
        }
    }
}
