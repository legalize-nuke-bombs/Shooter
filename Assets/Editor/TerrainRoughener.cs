using Shooter.Logging;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    public class TerrainRoughener : EditorWindow
    {
        private const float Fade = 0.5f;
        private const float Lacunarity = 2f;
        private const float Turn = 0.5f;
        private const float VarietyScale = 0.12f;
        private static readonly Journal Log = Logs.Here();

        private float amplitude = 0.15f;
        private float bump = 25f;
        private TerrainData captured;
        private float[,] ground;
        private float margin = 2.5f;
        private int octaves = 4;
        private float ridges = 0.6f;
        private float seed = 1f;
        private bool spare = true;
        private float variety = 0.7f;
        private float warp = 0.8f;

        private void OnEnable()
        {
            ground = null;
            captured = null;
        }

        private void OnGUI()
        {
            amplitude = EditorGUILayout.Slider("Amplitude, m", amplitude, 0f, 3f);
            bump = EditorGUILayout.Slider("Bump size, m", bump, 1f, 120f);
            octaves = EditorGUILayout.IntSlider("Octaves", octaves, 1, 6);

            EditorGUILayout.Space();
            ridges = EditorGUILayout.Slider("Ridges", ridges, 0f, 1f);
            warp = EditorGUILayout.Slider("Warp", warp, 0f, 2f);
            variety = EditorGUILayout.Slider("Variety", variety, 0f, 1f);
            seed = EditorGUILayout.FloatField("Seed", seed);

            EditorGUILayout.Space();
            spare = EditorGUILayout.Toggle("Spare placed objects", spare);
            using (new EditorGUI.DisabledScope(!spare))
            {
                margin = EditorGUILayout.Slider("Spare margin, m", margin, 0f, 12f);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(ground == null
                ? "Ground not captured yet"
                : $"Ground of {captured.name} captured, noise replaces the previous one");

            if (GUILayout.Button("Roughen")) Roughen();
            using (new EditorGUI.DisabledScope(ground == null))
            {
                if (GUILayout.Button("Restore captured ground")) Restore();
            }
        }

        [MenuItem("Tools/Roughen Terrain")]
        private static void Open()
        {
            GetWindow<TerrainRoughener>("Roughen Terrain");
        }

        private void Roughen()
        {
            Terrain terrain = Target();
            if (terrain == null) return;

            TerrainData data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            Vector3 size = data.size;
            Vector3 corner = terrain.transform.position;

            if (captured != data)
            {
                ground = null;
                captured = data;
            }

            if (ground == null) ground = data.GetHeights(0, 0, resolution, resolution);

            float[,] field = Field(resolution, corner, size);
            float[,] spared = spare ? Spared(resolution, corner, size) : null;
            float[,] heights = data.GetHeights(0, 0, resolution, resolution);
            float scale = Even(field, resolution);
            float highest = 0f;

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float shift = field[z, x] * scale;
                if (spared != null) shift *= 1f - spared[z, x];

                heights[z, x] = Mathf.Clamp01(ground[z, x] + shift / size.y);
                highest = Mathf.Max(highest, Mathf.Abs(shift));
            }

            Undo.RegisterCompleteObjectUndo(data, "Roughen terrain");
            data.SetHeights(0, 0, heights);
            EditorUtility.SetDirty(data);

            Log.Info(
                $"Terrain roughened: bump {bump}m over {octaves} octaves, ridges {ridges}, warp {warp}, variety {variety}, tallest shift {highest}m");
        }

        private void Restore()
        {
            if (ground == null || captured == null) return;

            Undo.RegisterCompleteObjectUndo(captured, "Restore terrain");
            captured.SetHeights(0, 0, ground);
            EditorUtility.SetDirty(captured);

            Log.Info($"Terrain {captured.name} restored to the ground captured before roughening");
        }

        private Terrain Target()
        {
            GameObject chosen = Selection.activeGameObject;
            Terrain terrain = chosen == null ? null : chosen.GetComponentInParent<Terrain>();
            if (terrain != null) return terrain;

            Terrain[] all = FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (all.Length == 1) return all[0];

            Log.Error(all.Length == 0
                ? "Scene has no terrain, nothing to roughen"
                : $"Scene has {all.Length} terrains, select the one to roughen");

            return null;
        }

        private float[,] Field(int resolution, Vector3 corner, Vector3 size)
        {
            float[,] field = new float[resolution, resolution];
            float stepX = size.x / (resolution - 1);
            float stepZ = size.z / (resolution - 1);
            float span = Mathf.Max(bump, 0.01f);

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                var point = new Vector2((corner.x + x * stepX) / span, (corner.z + z * stepZ) / span);
                field[z, x] = Shape(Warped(point)) * Rough(point);
            }

            return field;
        }

        private float Even(float[,] field, int resolution)
        {
            double total = 0d;

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
                total += field[z, x];

            float middle = (float)(total / (resolution * (double)resolution));
            float peak = 0f;

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                field[z, x] -= middle;
                peak = Mathf.Max(peak, Mathf.Abs(field[z, x]));
            }

            return peak <= 0f ? 0f : amplitude / peak;
        }

        private Vector2 Warped(Vector2 point)
        {
            if (warp <= 0f) return point;

            var shift = new Vector2(Noise(point + Offset(21)), Noise(point + Offset(22)));
            return point + shift * warp;
        }

        private float Shape(Vector2 point)
        {
            float sum = 0f;
            float weight = 0f;
            float share = 1f;
            Vector2 walk = point;

            for (int i = 0; i < octaves; i++)
            {
                float noise = Noise(walk + Offset(i));
                float crest = 1f - Mathf.Abs(noise);
                crest = crest * crest * 2f - 1f;

                sum += Mathf.Lerp(noise, crest, ridges) * share;
                weight += share;
                walk = Spun(walk) * Lacunarity;
                share *= Fade;
            }

            return weight <= 0f ? 0f : sum / weight;
        }

        private float Rough(Vector2 point)
        {
            if (variety <= 0f) return 1f;

            float calm = Noise(point * VarietyScale + Offset(31)) * 0.5f + 0.5f;
            return Mathf.Lerp(1f, Mathf.SmoothStep(0f, 1f, calm), variety);
        }

        private Vector2 Offset(int index)
        {
            return new Vector2(seed * 137.13f + index * 41.7f, seed * 291.71f + index * 79.3f);
        }

        private static float Noise(Vector2 point)
        {
            return Mathf.PerlinNoise(point.x, point.y) * 2f - 1f;
        }

        private static Vector2 Spun(Vector2 point)
        {
            float cosine = Mathf.Cos(Turn);
            float sine = Mathf.Sin(Turn);
            return new Vector2(point.x * cosine - point.y * sine, point.x * sine + point.y * cosine);
        }

        private float[,] Spared(int resolution, Vector3 corner, Vector3 size)
        {
            float[,] mask = new float[resolution, resolution];
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

            Log.Info($"Sparing {kept} placed objects with {margin}m margin, {huge} skipped as wider than {widest}m");
            return mask;
        }

        private void Cover(float[,] mask, Bounds bounds, Vector3 corner, float stepX, float stepZ, int resolution)
        {
            int fromX = Mathf.Clamp(Mathf.FloorToInt((bounds.min.x - margin - corner.x) / stepX), 0, resolution - 1);
            int tillX = Mathf.Clamp(Mathf.CeilToInt((bounds.max.x + margin - corner.x) / stepX), 0, resolution - 1);
            int fromZ = Mathf.Clamp(Mathf.FloorToInt((bounds.min.z - margin - corner.z) / stepZ), 0, resolution - 1);
            int tillZ = Mathf.Clamp(Mathf.CeilToInt((bounds.max.z + margin - corner.z) / stepZ), 0, resolution - 1);

            for (int z = fromZ; z <= tillZ; z++)
            for (int x = fromX; x <= tillX; x++)
            {
                float worldX = corner.x + x * stepX;
                float worldZ = corner.z + z * stepZ;
                float awayX = Mathf.Max(0f, Mathf.Max(bounds.min.x - worldX, worldX - bounds.max.x));
                float awayZ = Mathf.Max(0f, Mathf.Max(bounds.min.z - worldZ, worldZ - bounds.max.z));
                float away = Mathf.Sqrt(awayX * awayX + awayZ * awayZ);
                float cover = margin <= 0f ? away <= 0f ? 1f : 0f : Mathf.SmoothStep(1f, 0f, away / margin);

                if (cover > mask[z, x]) mask[z, x] = cover;
            }
        }
    }
}
