using Shooter.Logging;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    public class TerrainRoughener : EditorWindow
    {
        private static readonly Journal Log = Logs.Here();

        private const float Fade = 0.5f;

        private float amplitude = 0.15f;
        private float bump = 6f;
        private int octaves = 3;
        private float seed = 1f;
        private bool spare = true;
        private float margin = 2.5f;
        private float[,] ground;

        [MenuItem("Tools/Roughen Terrain")]
        private static void Open()
        {
            GetWindow<TerrainRoughener>("Roughen Terrain");
        }

        private void OnEnable()
        {
            ground = null;
        }

        private void OnGUI()
        {
            amplitude = EditorGUILayout.Slider("Amplitude, m", amplitude, 0f, 3f);
            bump = EditorGUILayout.Slider("Bump size, m", bump, 1f, 80f);
            octaves = EditorGUILayout.IntSlider("Octaves", octaves, 1, 5);
            seed = EditorGUILayout.FloatField("Seed", seed);

            EditorGUILayout.Space();
            spare = EditorGUILayout.Toggle("Spare placed objects", spare);
            using (new EditorGUI.DisabledScope(!spare))
                margin = EditorGUILayout.Slider("Spare margin, m", margin, 0f, 12f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(ground == null ? "Ground not captured yet" : "Ground captured, noise replaces the previous one");

            if (GUILayout.Button("Roughen")) Roughen();
            using (new EditorGUI.DisabledScope(ground == null))
                if (GUILayout.Button("Restore captured ground")) Restore();
        }

        private void Roughen()
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Log.Error("Scene has no active terrain, nothing to roughen");
                return;
            }

            TerrainData data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            Vector3 size = data.size;
            Vector3 corner = terrain.transform.position;

            if (ground == null) ground = data.GetHeights(0, 0, resolution, resolution);

            float[,] heights = data.GetHeights(0, 0, resolution, resolution);
            float[,] spared = spare ? Spared(resolution, corner, size) : null;
            float stepX = size.x / (resolution - 1);
            float stepZ = size.z / (resolution - 1);
            float highest = 0f;
            int moved = 0;

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float shift = Wave(corner.x + x * stepX, corner.z + z * stepZ) * amplitude;
                    if (spared != null) shift *= 1f - spared[z, x];

                    heights[z, x] = Mathf.Clamp01(ground[z, x] + shift / size.y);
                    if (Mathf.Abs(shift) < 0.001f) continue;

                    highest = Mathf.Max(highest, Mathf.Abs(shift));
                    moved++;
                }
            }

            Undo.RegisterCompleteObjectUndo(data, "Roughen terrain");
            data.SetHeights(0, 0, heights);
            EditorUtility.SetDirty(data);

            Log.Info("Terrain roughened: {} cells of {} moved, up to {}m, bump {}m over {} octaves",
                moved, resolution * resolution, highest, bump, octaves);
        }

        private void Restore()
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null || ground == null) return;

            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Restore terrain");
            terrain.terrainData.SetHeights(0, 0, ground);
            EditorUtility.SetDirty(terrain.terrainData);

            Log.Info("Terrain restored to the ground captured before roughening");
        }

        private float Wave(float x, float z)
        {
            float sum = 0f;
            float weight = 0f;
            float frequency = 1f / Mathf.Max(bump, 0.01f);
            float share = 1f;

            for (int i = 0; i < octaves; i++)
            {
                float noise = Mathf.PerlinNoise(x * frequency + seed * 137.13f, z * frequency + seed * 291.71f);
                sum += (noise - 0.5f) * 2f * share;
                weight += share;
                frequency *= 2f;
                share *= Fade;
            }

            return weight <= 0f ? 0f : sum / weight;
        }

        private float[,] Spared(int resolution, Vector3 corner, Vector3 size)
        {
            var mask = new float[resolution, resolution];
            Renderer[] found = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            float stepX = size.x / (resolution - 1);
            float stepZ = size.z / (resolution - 1);
            float widest = size.x * 0.25f;
            int kept = 0;
            int huge = 0;

            foreach (Renderer renderer in found)
            {
                if (renderer.GetComponentInParent<Terrain>() != null) continue;

                Bounds bounds = renderer.bounds;
                if (bounds.size.x > widest || bounds.size.z > widest)
                {
                    huge++;
                    continue;
                }

                Cover(mask, bounds, corner, stepX, stepZ, resolution);
                kept++;
            }

            Log.Info("Sparing {} placed objects with {}m margin, {} skipped as wider than {}m",
                kept, margin, huge, widest);
            return mask;
        }

        private void Cover(float[,] mask, Bounds bounds, Vector3 corner, float stepX, float stepZ, int resolution)
        {
            int fromX = Mathf.Clamp(Mathf.FloorToInt((bounds.min.x - margin - corner.x) / stepX), 0, resolution - 1);
            int tillX = Mathf.Clamp(Mathf.CeilToInt((bounds.max.x + margin - corner.x) / stepX), 0, resolution - 1);
            int fromZ = Mathf.Clamp(Mathf.FloorToInt((bounds.min.z - margin - corner.z) / stepZ), 0, resolution - 1);
            int tillZ = Mathf.Clamp(Mathf.CeilToInt((bounds.max.z + margin - corner.z) / stepZ), 0, resolution - 1);

            for (int z = fromZ; z <= tillZ; z++)
            {
                for (int x = fromX; x <= tillX; x++)
                {
                    float worldX = corner.x + x * stepX;
                    float worldZ = corner.z + z * stepZ;
                    float awayX = Mathf.Max(0f, Mathf.Max(bounds.min.x - worldX, worldX - bounds.max.x));
                    float awayZ = Mathf.Max(0f, Mathf.Max(bounds.min.z - worldZ, worldZ - bounds.max.z));
                    float away = Mathf.Sqrt(awayX * awayX + awayZ * awayZ);
                    float cover = margin <= 0f ? (away <= 0f ? 1f : 0f) : Mathf.SmoothStep(1f, 0f, away / margin);

                    if (cover > mask[z, x]) mask[z, x] = cover;
                }
            }
        }
    }
}
