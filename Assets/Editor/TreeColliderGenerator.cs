using System.Collections.Generic;
using System.Linq;
using Shooter.Logging;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    public static class TreeColliderGenerator
    {
        private static readonly Journal Log = Logs.Here();

        private const string Menu = "Tools/Generate Tree Colliders";
        private const string Mark = "Collider_";

        private const float WeldStep = 0.001f;
        private const float SliceLength = 3.5f;
        private const float LeastRadius = 0.04f;
        private const float LeastLength = 0.5f;
        private const float FootReach = 0.5f;
        private const int Budget = 10;
        private const int LeastSlicePoints = 4;

        [MenuItem(Menu)]
        private static void Generate()
        {
            List<string> paths = Prefabs().ToList();
            if (paths.Count == 0)
            {
                Log.Warn("Nothing selected in the project window, pick a folder or a prefab");
                return;
            }

            int dressed = 0;
            int placed = 0;

            for (int i = 0; i < paths.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar(Menu, paths[i], (float) i / paths.Count))
                    break;

                int grown = Dress(paths[i]);
                if (grown <= 0) continue;

                dressed++;
                placed += grown;
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();

            Log.Warn("Looked at {} prefabs, dressed {} trees with {} capsules", paths.Count, dressed, placed);
        }

        [MenuItem(Menu, true)]
        private static bool Ready()
        {
            return Selection.assetGUIDs.Length > 0;
        }

        private static IEnumerable<string> Prefabs()
        {
            string[] folders = Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            return AssetDatabase.FindAssets("t:Prefab", folders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct();
        }

        private static int Dress(string path)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);

            try
            {
                int grown = Grow(prefab, path);
                if (grown > 0) PrefabUtility.SaveAsPrefabAsset(prefab, path);

                return grown;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static int Grow(GameObject prefab, string path)
        {
            MeshRenderer renderer = Detailed(prefab);
            Mesh mesh = renderer == null ? null : Meshed(renderer);

            if (mesh == null)
            {
                Log.Warn("Prefab {} has no mesh to trace, skipped", path);
                return 0;
            }

            if (!mesh.isReadable)
            {
                Log.Error("Mesh {} of prefab {} is not readable from editor code, skipped without touching the import", mesh.name, path);
                return 0;
            }

            List<int> barks = Barks(renderer, mesh);
            if (barks.Count == 0)
            {
                Log.Warn("Prefab {} has no bark material on mesh {}, skipped", path, mesh.name);
                return 0;
            }

            Matrix4x4 intoRoot = prefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            List<List<Vector3>> components = Components(mesh, barks, intoRoot);

            if (components.Count == 0)
            {
                Log.Warn("Prefab {} has bark materials but no bark triangles, skipped", path);
                return 0;
            }

            List<Limb> limbs = Limbs(components, out int twigs);
            List<Capsule> chosen = Chosen(limbs, out int fitted);

            if (chosen.Count == 0)
            {
                Log.Warn("Prefab {} yielded no capsules, left untouched", path);
                return 0;
            }

            Clear(prefab);

            Log.Info("Tree {}: {} bark submeshes on mesh {}, {} components, {} twigs dropped, {} limbs kept, {} capsules fitted, {} placed",
                prefab.name, barks.Count, mesh.name, components.Count, twigs, limbs.Count, fitted, chosen.Count);

            Place(prefab, chosen);

            return chosen.Count;
        }

        private static MeshRenderer Detailed(GameObject prefab)
        {
            LODGroup lods = prefab.GetComponentInChildren<LODGroup>(true);

            if (lods != null)
            {
                LOD[] steps = lods.GetLODs();

                if (steps.Length > 0)
                {
                    foreach (Renderer renderer in steps[0].renderers)
                    {
                        if (renderer is MeshRenderer detailed && Meshed(detailed) != null) return detailed;
                    }
                }
            }

            MeshRenderer best = null;
            int most = 0;

            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;

                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                if (filter.sharedMesh.vertexCount <= most) continue;

                most = filter.sharedMesh.vertexCount;
                best = renderer;
            }

            return best;
        }

        private static Mesh Meshed(MeshRenderer renderer)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();

            return filter == null ? null : filter.sharedMesh;
        }

        private static List<int> Barks(MeshRenderer renderer, Mesh mesh)
        {
            var found = new List<int>();
            Material[] materials = renderer.sharedMaterials;
            int count = Mathf.Min(materials.Length, mesh.subMeshCount);

            for (int submesh = 0; submesh < count; submesh++)
            {
                Material material = materials[submesh];
                if (material == null) continue;

                if (material.name.ToLowerInvariant().Contains("bark")) found.Add(submesh);
            }

            return found;
        }

        private static List<List<Vector3>> Components(Mesh mesh, List<int> barks, Matrix4x4 intoRoot)
        {
            Vector3[] meshVertices = mesh.vertices;
            var welder = new Welder();

            foreach (int submesh in barks)
            {
                if (mesh.GetTopology(submesh) != MeshTopology.Triangles) continue;

                int[] corners = mesh.GetTriangles(submesh);

                for (int corner = 0; corner < corners.Length; corner += 3)
                {
                    int first = welder.Meet(intoRoot.MultiplyPoint3x4(meshVertices[corners[corner]]));
                    int second = welder.Meet(intoRoot.MultiplyPoint3x4(meshVertices[corners[corner + 1]]));
                    int third = welder.Meet(intoRoot.MultiplyPoint3x4(meshVertices[corners[corner + 2]]));

                    welder.Join(first, second);
                    welder.Join(first, third);
                }
            }

            return welder.Bunched();
        }

        private static List<Limb> Limbs(List<List<Vector3>> components, out int twigs)
        {
            var limbs = new List<Limb>();

            foreach (List<Vector3> points in components)
            {
                Vector3 middle = Middle(points);
                Vector3 axis = Axis(points, middle);
                Capsule whole = Fitted(points, axis, false);

                limbs.Add(new Limb
                {
                    Points = points,
                    Axis = axis,
                    Radius = whole.Radius,
                    Length = whole.Length,
                    FootHeight = points.Min(point => point.y)
                });
            }

            float floor = limbs.Min(limb => limb.FootHeight);
            Limb trunk = limbs
                .Where(limb => limb.FootHeight <= floor + FootReach)
                .OrderByDescending(limb => limb.Radius * limb.Radius * limb.Length)
                .First();
            trunk.Trunk = true;

            int before = limbs.Count;
            limbs = limbs
                .Where(limb => limb.Trunk || (limb.Radius >= LeastRadius && limb.Length >= LeastLength))
                .ToList();
            twigs = before - limbs.Count;

            return limbs;
        }

        private static List<Capsule> Chosen(List<Limb> limbs, out int fitted)
        {
            var trunkFits = new List<Capsule>();
            var branchFits = new List<Capsule>();

            foreach (Limb limb in limbs)
            {
                foreach (Capsule capsule in Sliced(limb))
                {
                    if (capsule.Radius < 0.001f || capsule.Length < 0.001f) continue;

                    if (capsule.Trunk) trunkFits.Add(capsule);
                    else branchFits.Add(capsule);
                }
            }

            fitted = trunkFits.Count + branchFits.Count;

            List<Capsule> chosen = trunkFits
                .OrderBy(capsule => capsule.Center.y)
                .Take(Budget)
                .ToList();

            chosen.AddRange(branchFits
                .OrderByDescending(capsule => capsule.Volume)
                .Take(Budget - chosen.Count));

            return chosen;
        }

        private static List<Capsule> Sliced(Limb limb)
        {
            Vector3 middle = Middle(limb.Points);
            float low = float.MaxValue;
            float high = float.MinValue;

            foreach (Vector3 point in limb.Points)
            {
                float along = Vector3.Dot(point - middle, limb.Axis);
                if (along < low) low = along;
                if (along > high) high = along;
            }

            float length = high - low;
            int count = Mathf.Max(1, Mathf.RoundToInt(length / SliceLength));
            if (count == 1) return new List<Capsule> { Fitted(limb.Points, limb.Axis, limb.Trunk) };

            var slices = new List<Vector3>[count];
            for (int i = 0; i < count; i++) slices[i] = new List<Vector3>();

            foreach (Vector3 point in limb.Points)
            {
                float along = Vector3.Dot(point - middle, limb.Axis);
                int at = Mathf.Clamp((int) ((along - low) / length * count), 0, count - 1);
                slices[at].Add(point);
            }

            var capsules = new List<Capsule>();

            foreach (List<Vector3> slice in slices)
            {
                if (slice.Count < LeastSlicePoints) continue;

                Vector3 sliceMiddle = Middle(slice);
                Vector3 sliceAxis = Axis(slice, sliceMiddle);
                capsules.Add(Fitted(slice, sliceAxis, limb.Trunk));
            }

            return capsules;
        }

        private static Capsule Fitted(List<Vector3> points, Vector3 axis, bool trunk)
        {
            Vector3 middle = Middle(points);
            float low = float.MaxValue;
            float high = float.MinValue;
            var spreads = new List<float>(points.Count);

            foreach (Vector3 point in points)
            {
                Vector3 offset = point - middle;
                float along = Vector3.Dot(offset, axis);
                if (along < low) low = along;
                if (along > high) high = along;

                spreads.Add((offset - axis * along).magnitude);
            }

            spreads.Sort();

            return new Capsule
            {
                Center = middle + axis * ((low + high) * 0.5f),
                Axis = axis,
                Radius = spreads[spreads.Count / 2],
                Length = high - low,
                Trunk = trunk
            };
        }

        private static Vector3 Middle(List<Vector3> points)
        {
            Vector3 sum = Vector3.zero;
            foreach (Vector3 point in points) sum += point;

            return sum / points.Count;
        }

        private static Vector3 Axis(List<Vector3> points, Vector3 middle)
        {
            Vector3 start = Sweep(points, middle);

            double spreadXX = 0;
            double spreadXY = 0;
            double spreadXZ = 0;
            double spreadYY = 0;
            double spreadYZ = 0;
            double spreadZZ = 0;

            foreach (Vector3 point in points)
            {
                double x = point.x - middle.x;
                double y = point.y - middle.y;
                double z = point.z - middle.z;

                spreadXX += x * x;
                spreadXY += x * y;
                spreadXZ += x * z;
                spreadYY += y * y;
                spreadYZ += y * z;
                spreadZZ += z * z;
            }

            Vector3 axis = start;

            for (int turn = 0; turn < 32; turn++)
            {
                double spunX = spreadXX * axis.x + spreadXY * axis.y + spreadXZ * axis.z;
                double spunY = spreadXY * axis.x + spreadYY * axis.y + spreadYZ * axis.z;
                double spunZ = spreadXZ * axis.x + spreadYZ * axis.y + spreadZZ * axis.z;
                double size = System.Math.Sqrt(spunX * spunX + spunY * spunY + spunZ * spunZ);
                if (size < 0.000000000001) return start;

                axis = new Vector3((float) (spunX / size), (float) (spunY / size), (float) (spunZ / size));
            }

            return axis.y < 0f ? -axis : axis;
        }

        private static Vector3 Sweep(List<Vector3> points, Vector3 middle)
        {
            Vector3 far = Farthest(points, middle);
            Vector3 opposite = Farthest(points, far);
            Vector3 heading = opposite - far;

            return heading.sqrMagnitude < 0.000001f ? Vector3.up : heading.normalized;
        }

        private static Vector3 Farthest(List<Vector3> points, Vector3 from)
        {
            Vector3 found = from;
            float best = -1f;

            foreach (Vector3 point in points)
            {
                float away = (point - from).sqrMagnitude;
                if (away <= best) continue;

                best = away;
                found = point;
            }

            return found;
        }

        private static void Clear(GameObject prefab)
        {
            Transform root = prefab.transform;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.name.StartsWith(Mark)) Object.DestroyImmediate(child.gameObject);
            }

            foreach (BoxCollider box in prefab.GetComponents<BoxCollider>())
            {
                Object.DestroyImmediate(box);
            }
        }

        private static void Place(GameObject prefab, List<Capsule> chosen)
        {
            int trunks = 0;
            int branches = 0;

            foreach (Capsule capsule in chosen)
            {
                string name = capsule.Trunk ? Mark + "Trunk_" + trunks++ : Mark + "Branch_" + branches++;

                var child = new GameObject(name);
                child.layer = prefab.layer;
                child.transform.SetParent(prefab.transform, false);
                child.transform.localPosition = capsule.Center;
                child.transform.localRotation = Quaternion.FromToRotation(Vector3.up, capsule.Axis);

                CapsuleCollider collider = child.AddComponent<CapsuleCollider>();
                collider.direction = 1;
                collider.radius = capsule.Radius;
                collider.height = capsule.Length + capsule.Radius * 2f;

                Log.Info("Tree {}: {} radius {} m, height {} m, center y {} m",
                    prefab.name, name, Round(capsule.Radius), Round(collider.height), Round(capsule.Center.y));
            }
        }

        private static float Round(float value)
        {
            return Mathf.Round(value * 100f) / 100f;
        }

        private sealed class Welder
        {
            private readonly Dictionary<Vector3Int, int> cells = new Dictionary<Vector3Int, int>();
            private readonly List<Vector3> positions = new List<Vector3>();
            private readonly List<int> parents = new List<int>();

            public int Meet(Vector3 position)
            {
                var cell = new Vector3Int(
                    Mathf.RoundToInt(position.x / WeldStep),
                    Mathf.RoundToInt(position.y / WeldStep),
                    Mathf.RoundToInt(position.z / WeldStep));

                if (cells.TryGetValue(cell, out int known)) return known;

                int fresh = positions.Count;
                cells.Add(cell, fresh);
                positions.Add(position);
                parents.Add(fresh);

                return fresh;
            }

            public void Join(int one, int another)
            {
                int oneRoot = Root(one);
                int anotherRoot = Root(another);
                if (oneRoot != anotherRoot) parents[anotherRoot] = oneRoot;
            }

            public List<List<Vector3>> Bunched()
            {
                var bunches = new Dictionary<int, List<Vector3>>();

                for (int id = 0; id < positions.Count; id++)
                {
                    int root = Root(id);

                    if (!bunches.TryGetValue(root, out List<Vector3> bunch))
                    {
                        bunch = new List<Vector3>();
                        bunches.Add(root, bunch);
                    }

                    bunch.Add(positions[id]);
                }

                return bunches.Values.ToList();
            }

            private int Root(int id)
            {
                while (parents[id] != id)
                {
                    parents[id] = parents[parents[id]];
                    id = parents[id];
                }

                return id;
            }
        }

        private sealed class Limb
        {
            public List<Vector3> Points;
            public Vector3 Axis;
            public float Radius;
            public float Length;
            public float FootHeight;
            public bool Trunk;
        }

        private sealed class Capsule
        {
            public Vector3 Center;
            public Vector3 Axis;
            public float Radius;
            public float Length;
            public bool Trunk;

            public float Volume => Radius * Radius * (Length + Radius);
        }
    }
}
