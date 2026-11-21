using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class TracedTerrainDetails : MonoBehaviour, IRayTracedInstances
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<Camera, DetailView> views = new();
        private readonly Dictionary<PatchKey, List<PatchMeshPlacements>> patches = new();
        private readonly List<Camera> forgottenCameras = new();
        private readonly List<PatchKey> forgottenPatches = new();
        private readonly HashSet<PatchKey> wantedPatches = new();
        private readonly List<ViewPatch> viewPatches = new();

        private void OnDestroy()
        {
            foreach (DetailView view in views.Values)
            {
                view.Dispose();
            }

            views.Clear();
            patches.Clear();
        }

        public void AddInstancesTo(AccelerationStructureBuilder builder, HDCamera hdCamera)
        {
            if (!hdCamera.volumeStack.GetComponent<PathTracing>().enable.value) return;

            Camera camera = hdCamera.camera;
            if (!views.TryGetValue(camera, out DetailView view))
            {
                ForgetDestroyedCameras();
                view = new DetailView();
                views[camera] = view;
            }

            CollectViewPatches(camera.transform.position);
            if (!view.Shows(viewPatches)) Rebuild(view);

            foreach (DetailMesh mesh in view.Meshes.Values)
            {
                if (mesh.Count == 0) continue;

                builder.AddInstances(mesh.Renderer, mesh.Mesh, mesh.ObjectToWorld.GetSubArray(0, mesh.Count));
            }
        }

        private void CollectViewPatches(Vector3 cameraPosition)
        {
            viewPatches.Clear();
            foreach (Terrain terrain in Registers.Instance.GetAll<Terrain>(RegisterInactivePolicy.Exclude))
            {
                if (!terrain.drawTreesAndFoliage || terrain.terrainData == null) continue;

                TerrainData data = terrain.terrainData;
                Vector2 patchSize = new(data.size.x / data.detailPatchCount, data.size.z / data.detailPatchCount);
                Vector3 local = cameraPosition - terrain.transform.position;
                Vector2Int patch = new(Mathf.FloorToInt(local.x / patchSize.x), Mathf.FloorToInt(local.z / patchSize.y));
                viewPatches.Add(new ViewPatch(terrain, patch));
            }
        }

        private void Rebuild(DetailView view)
        {
            view.Remember(viewPatches);
            view.Clear();
            wantedPatches.Clear();

            foreach (ViewPatch viewPatch in viewPatches)
            {
                Terrain terrain = viewPatch.Terrain;
                TerrainData data = terrain.terrainData;
                Vector2 patchSize = new(data.size.x / data.detailPatchCount, data.size.z / data.detailPatchCount);
                float radius = terrain.detailObjectDistance;
                Vector2 center = new((viewPatch.Patch.x + 0.5f) * patchSize.x, (viewPatch.Patch.y + 0.5f) * patchSize.y);
                int minX = Mathf.Max(0, Mathf.FloorToInt((center.x - radius) / patchSize.x));
                int maxX = Mathf.Min(data.detailPatchCount - 1, Mathf.FloorToInt((center.x + radius) / patchSize.x));
                int minY = Mathf.Max(0, Mathf.FloorToInt((center.y - radius) / patchSize.y));
                int maxY = Mathf.Min(data.detailPatchCount - 1, Mathf.FloorToInt((center.y + radius) / patchSize.y));

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (!Reaches(center, radius, x, y, patchSize)) continue;

                        PatchKey key = new(terrain, new Vector2Int(x, y));
                        wantedPatches.Add(key);
                        if (!patches.TryGetValue(key, out List<PatchMeshPlacements> placements))
                        {
                            placements = CollectPatch(terrain, x, y);
                            patches[key] = placements;
                        }

                        foreach (PatchMeshPlacements placement in placements)
                        {
                            view.Add(placement);
                        }
                    }
                }
            }

            ForgetUnwantedPatches();
            Log.Debug($"Detail view rebuilt: {wantedPatches.Count} patches, {view.InstanceCount} instances in {view.Meshes.Count} meshes");
        }

        private static bool Reaches(Vector2 center, float radius, int x, int y, Vector2 patchSize)
        {
            float nearestX = Mathf.Clamp(center.x, x * patchSize.x, (x + 1) * patchSize.x);
            float nearestY = Mathf.Clamp(center.y, y * patchSize.y, (y + 1) * patchSize.y);
            return (new Vector2(nearestX, nearestY) - center).sqrMagnitude <= radius * radius;
        }

        private static List<PatchMeshPlacements> CollectPatch(Terrain terrain, int patchX, int patchY)
        {
            TerrainData data = terrain.terrainData;
            DetailPrototype[] prototypes = data.detailPrototypes;
            int resolution = data.detailResolutionPerPatch;
            int[] layers = data.GetSupportedLayers(new Vector2Int(patchX * resolution, patchY * resolution), new Vector2Int(resolution, resolution));
            Vector3 origin = terrain.transform.position;
            var placements = new List<PatchMeshPlacements>();

            foreach (int layer in layers)
            {
                DetailPrototype prototype = prototypes[layer];
                if (!prototype.usePrototypeMesh || prototype.prototype == null) continue;

                DetailInstanceTransform[] instances = data.ComputeDetailInstanceTransforms(patchX, patchY, layer, terrain.detailObjectDensity, out _);
                if (instances.Length == 0) continue;

                GameObject prefab = prototype.prototype;
                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;

                    Matrix4x4 inPrefab = prefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                    var objectToWorld = new Matrix4x4[instances.Length];
                    for (int i = 0; i < instances.Length; i++)
                    {
                        DetailInstanceTransform instance = instances[i];
                        Vector3 position = origin + new Vector3(instance.posX, instance.posY, instance.posZ);
                        Quaternion rotation = Quaternion.AngleAxis(instance.rotationY * Mathf.Rad2Deg, Vector3.up);
                        Vector3 scale = new(instance.scaleXZ, instance.scaleY, instance.scaleXZ);
                        objectToWorld[i] = Matrix4x4.TRS(position, rotation, scale) * inPrefab;
                    }

                    placements.Add(new PatchMeshPlacements(renderer, filter.sharedMesh, objectToWorld));
                }
            }

            return placements;
        }

        private void ForgetUnwantedPatches()
        {
            forgottenPatches.Clear();
            foreach (PatchKey key in patches.Keys)
            {
                if (!wantedPatches.Contains(key)) forgottenPatches.Add(key);
            }

            foreach (PatchKey key in forgottenPatches)
            {
                patches.Remove(key);
            }
        }

        private void ForgetDestroyedCameras()
        {
            forgottenCameras.Clear();
            foreach (KeyValuePair<Camera, DetailView> pair in views)
            {
                if (pair.Key == null) forgottenCameras.Add(pair.Key);
            }

            foreach (Camera camera in forgottenCameras)
            {
                views[camera].Dispose();
                views.Remove(camera);
            }
        }

        private readonly struct PatchKey : System.IEquatable<PatchKey>
        {
            private readonly Terrain terrain;
            private readonly Vector2Int patch;

            public PatchKey(Terrain terrain, Vector2Int patch)
            {
                this.terrain = terrain;
                this.patch = patch;
            }

            public bool Equals(PatchKey other)
            {
                return terrain == other.terrain && patch == other.patch;
            }

            public override bool Equals(object other)
            {
                return other is PatchKey key && Equals(key);
            }

            public override int GetHashCode()
            {
                return System.HashCode.Combine(terrain.GetEntityId(), patch);
            }
        }

        private readonly struct ViewPatch
        {
            public readonly Terrain Terrain;
            public readonly Vector2Int Patch;

            public ViewPatch(Terrain terrain, Vector2Int patch)
            {
                Terrain = terrain;
                Patch = patch;
            }
        }

        private sealed class PatchMeshPlacements
        {
            public readonly Renderer Renderer;
            public readonly Mesh Mesh;
            public readonly Matrix4x4[] ObjectToWorld;

            public PatchMeshPlacements(Renderer renderer, Mesh mesh, Matrix4x4[] objectToWorld)
            {
                Renderer = renderer;
                Mesh = mesh;
                ObjectToWorld = objectToWorld;
            }
        }

        private sealed class DetailMesh
        {
            public readonly Renderer Renderer;
            public readonly Mesh Mesh;
            public NativeArray<Matrix4x4> ObjectToWorld;
            public int Count;

            public DetailMesh(Renderer renderer, Mesh mesh)
            {
                Renderer = renderer;
                Mesh = mesh;
            }

            public void Append(Matrix4x4[] objectToWorld)
            {
                int needed = Count + objectToWorld.Length;
                if (needed > ObjectToWorld.Length)
                {
                    var grown = new NativeArray<Matrix4x4>(Mathf.Max(needed, ObjectToWorld.Length * 2), Allocator.Persistent);
                    if (ObjectToWorld.IsCreated)
                    {
                        NativeArray<Matrix4x4>.Copy(ObjectToWorld, grown, Count);
                        ObjectToWorld.Dispose();
                    }

                    ObjectToWorld = grown;
                }

                NativeArray<Matrix4x4>.Copy(objectToWorld, 0, ObjectToWorld, Count, objectToWorld.Length);
                Count = needed;
            }

            public void Dispose()
            {
                if (ObjectToWorld.IsCreated) ObjectToWorld.Dispose();
            }
        }

        private sealed class DetailView
        {
            public readonly Dictionary<(Renderer, Mesh), DetailMesh> Meshes = new();
            private readonly List<ViewPatch> shown = new();

            public int InstanceCount
            {
                get
                {
                    int count = 0;
                    foreach (DetailMesh mesh in Meshes.Values)
                    {
                        count += mesh.Count;
                    }

                    return count;
                }
            }

            public bool Shows(List<ViewPatch> patches)
            {
                if (patches.Count != shown.Count) return false;

                for (int i = 0; i < patches.Count; i++)
                {
                    if (patches[i].Terrain != shown[i].Terrain || patches[i].Patch != shown[i].Patch) return false;
                }

                return true;
            }

            public void Remember(List<ViewPatch> patches)
            {
                shown.Clear();
                shown.AddRange(patches);
            }

            public void Clear()
            {
                foreach (DetailMesh mesh in Meshes.Values)
                {
                    mesh.Count = 0;
                }
            }

            public void Add(PatchMeshPlacements placement)
            {
                if (!Meshes.TryGetValue((placement.Renderer, placement.Mesh), out DetailMesh mesh))
                {
                    mesh = new DetailMesh(placement.Renderer, placement.Mesh);
                    Meshes[(placement.Renderer, placement.Mesh)] = mesh;
                }

                mesh.Append(placement.ObjectToWorld);
            }

            public void Dispose()
            {
                foreach (DetailMesh mesh in Meshes.Values)
                {
                    mesh.Dispose();
                }

                Meshes.Clear();
            }
        }
    }
}
