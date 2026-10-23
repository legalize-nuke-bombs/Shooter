using System.Collections.Generic;
using Shooter.Game;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Editor
{
    // Shore markers tell a resident where the water is, the way people see a shore: the near water line in detail, the far
    // one in a few points. Small markers stand close together and are seen from close by, large ones far apart and from afar
    public class BuildLakeShoresTool : EditorWindow
    {
        private const int MaxLakeCells = 2_000_000;
        private static readonly Journal Log = Logs.Here();
        private static readonly Vector2Int[] Neighbours = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        [SerializeField] private float gridStep = 2f;
        [SerializeField] private float smallSpacing = 25f;
        [SerializeField] private float mediumSpacing = 100f;
        [SerializeField] private float largeSpacing = 300f;

        private string lastRunSummary;
        private SerializedObject serializedWindow;

        [MenuItem("Tools/Build Lake Shores")]
        private static void OpenWindow()
        {
            GetWindow<BuildLakeShoresTool>("Build Lake Shores");
        }

        private void OnEnable()
        {
            serializedWindow = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedWindow.Update();
            DrawField(nameof(gridStep), "Water line step, m");
            DrawField(nameof(smallSpacing), "Small markers every, m");
            DrawField(nameof(mediumSpacing), "Medium markers every, m");
            DrawField(nameof(largeSpacing), "Large markers every, m");
            serializedWindow.ApplyModifiedProperties();

            EditorGUILayout.LabelField("A lake is an object standing in its water and named as the lake is called. Its shore markers become its " +
                                       "children and say \"<lake name> shore\"; building again replaces them", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            GameObject[] lakes = Selection.gameObjects;
            EditorGUILayout.LabelField(lakes.Length == 0 ? "Select the lakes to build the shores of" : $"{lakes.Length} selected lakes");

            using (new EditorGUI.DisabledScope(lakes.Length == 0))
            {
                if (GUILayout.Button("Build shores"))
                {
                    lastRunSummary = BuildShores(lakes);
                }
            }

            if (!string.IsNullOrEmpty(lastRunSummary))
            {
                EditorGUILayout.LabelField(lastRunSummary, EditorStyles.wordWrappedLabel);
            }
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void DrawField(string propertyName, string label)
        {
            EditorGUILayout.PropertyField(serializedWindow.FindProperty(propertyName), new GUIContent(label));
        }

        private string BuildShores(GameObject[] lakes)
        {
            if (EditorApplication.isPlaying)
            {
                Log.Warn("Shores are built in edit mode only, stop the play mode first");
                return "Stop the play mode first";
            }

            Water water = Object.FindAnyObjectByType<Water>();
            if (water == null)
            {
                Log.Warn("There is no water in the open scenes, no shore to build");
                return "No water in the open scenes";
            }

            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude);
            var summaries = new List<string>();
            foreach (GameObject lake in lakes)
            {
                summaries.Add(BuildShore(lake, water, terrains));
            }

            return string.Join("\n", summaries);
        }

        private string BuildShore(GameObject lake, Water water, Terrain[] terrains)
        {
            HashSet<Vector2Int> lakeCells = FindLakeCells(lake.transform.position, water, terrains);
            if (lakeCells == null)
            {
                Log.Warn($"Lake {lake.name} does not stand in water, its shore is not built");
                return $"{lake.name}: does not stand in water";
            }

            List<Vector3> waterLine = FindWaterLine(lakeCells, water.transform.position.y);
            List<Vector3> large = Thin(waterLine, largeSpacing, new List<Vector3>());
            List<Vector3> medium = Thin(waterLine, mediumSpacing, large);
            List<Vector3> all = Thin(waterLine, smallSpacing, medium);

            string description = $"{lake.name} shore";
            RemoveMarkers(lake, description);
            for (int i = 0; i < all.Count; i++)
            {
                DigestibleSize size = i < large.Count ? DigestibleSize.Large : i < medium.Count ? DigestibleSize.Medium : DigestibleSize.Small;
                CreateMarker(lake, all[i], size, description);
            }

            EditorSceneManager.MarkSceneDirty(lake.scene);

            string summary = $"{lake.name}: {large.Count} large, {medium.Count - large.Count} medium and {all.Count - medium.Count} small markers " +
                             $"along {waterLine.Count * gridStep / 1000f:F1} km of water line";
            Log.Info(summary);
            return summary;
        }

        private HashSet<Vector2Int> FindLakeCells(Vector3 seed, Water water, Terrain[] terrains)
        {
            var start = new Vector2Int(Mathf.RoundToInt(seed.x / gridStep), Mathf.RoundToInt(seed.z / gridStep));
            if (!IsWater(start, water, terrains)) return null;

            var lakeCells = new HashSet<Vector2Int> { start };
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(start);
            while (frontier.Count > 0)
            {
                Vector2Int cell = frontier.Dequeue();
                foreach (Vector2Int neighbour in Neighbours)
                {
                    Vector2Int next = cell + neighbour;
                    if (lakeCells.Contains(next) || !IsWater(next, water, terrains)) continue;

                    lakeCells.Add(next);
                    frontier.Enqueue(next);
                }

                if (lakeCells.Count > MaxLakeCells)
                {
                    Log.Warn($"The water around {seed} has more than {MaxLakeCells} cells, its shore is cut short");
                    break;
                }
            }

            return lakeCells;
        }

        private bool IsWater(Vector2Int cell, Water water, Terrain[] terrains)
        {
            Vector3 point = ToPoint(cell, 0f);
            float ground = float.NegativeInfinity;
            foreach (Terrain terrain in terrains)
            {
                Vector3 origin = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (point.x < origin.x || point.z < origin.z || point.x > origin.x + size.x || point.z > origin.z + size.z) continue;

                ground = Mathf.Max(ground, terrain.SampleHeight(point) + origin.y);
            }

            return !float.IsNegativeInfinity(ground) && water.GetDepthAt(new Vector3(point.x, ground, point.z)) > 0f;
        }

        // The lake cells next to dry ground, row by row so the markers come out the same on every build
        private List<Vector3> FindWaterLine(HashSet<Vector2Int> lakeCells, float waterLevel)
        {
            var edge = new List<Vector2Int>();
            foreach (Vector2Int cell in lakeCells)
            {
                foreach (Vector2Int neighbour in Neighbours)
                {
                    if (lakeCells.Contains(cell + neighbour)) continue;

                    edge.Add(cell);
                    break;
                }
            }

            edge.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            return edge.ConvertAll(cell => ToPoint(cell, waterLevel));
        }

        // The points of the water line that stand at least the spacing from each other and from the ones already standing
        private static List<Vector3> Thin(List<Vector3> waterLine, float spacing, List<Vector3> standing)
        {
            var kept = new List<Vector3>(standing);
            float spacingSqr = spacing * spacing;
            foreach (Vector3 point in waterLine)
            {
                bool tooClose = false;
                foreach (Vector3 other in kept)
                {
                    if ((other - point).sqrMagnitude >= spacingSqr) continue;

                    tooClose = true;
                    break;
                }

                if (!tooClose) kept.Add(point);
            }

            return kept;
        }

        private static void RemoveMarkers(GameObject lake, string description)
        {
            for (int i = lake.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = lake.transform.GetChild(i).gameObject;
                if (child.TryGetComponent(out MainDigestible digestible) && digestible.Digest(DigestionDetail.Brief) == description)
                {
                    Object.DestroyImmediate(child);
                }
            }
        }

        private static void CreateMarker(GameObject lake, Vector3 position, DigestibleSize size, string description)
        {
            var marker = new GameObject($"{description} ({size.ToString().ToLowerInvariant()})");
            marker.transform.SetParent(lake.transform, false);
            marker.transform.position = position;

            var digestible = new SerializedObject(marker.AddComponent<MainDigestible>());
            digestible.FindProperty(MainDigestible.ContentField).stringValue = description;
            digestible.FindProperty(MainDigestible.SizeField).enumValueIndex = (int)size;
            digestible.ApplyModifiedPropertiesWithoutUndo();

            var gizmo = new SerializedObject(marker.AddComponent<TextGizmo>());
            gizmo.FindProperty(TextGizmo.LabelField).stringValue = size.ToString().ToLowerInvariant();
            gizmo.ApplyModifiedPropertiesWithoutUndo();
        }

        private Vector3 ToPoint(Vector2Int cell, float height)
        {
            return new Vector3(cell.x * gridStep, height, cell.y * gridStep);
        }
    }
}
