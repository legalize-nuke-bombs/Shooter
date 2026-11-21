using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class TracedTerrainTrees : MonoBehaviour, IRayTracedInstances
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<Terrain, List<TreeMesh>> meshesByTerrain = new();
        private readonly List<Terrain> forgotten = new();

        private void OnDestroy()
        {
            foreach (List<TreeMesh> meshes in meshesByTerrain.Values)
            {
                Dispose(meshes);
            }

            meshesByTerrain.Clear();
        }

        public void AddInstancesTo(AccelerationStructureBuilder builder, HDCamera hdCamera)
        {
            foreach (Terrain terrain in Registers.Instance.GetAll<Terrain>(RegisterInactivePolicy.Exclude))
            {
                if (!terrain.drawTreesAndFoliage) continue;

                if (!meshesByTerrain.TryGetValue(terrain, out List<TreeMesh> meshes))
                {
                    ForgetDestroyedTerrains();
                    meshes = CollectTreeMeshes(terrain);
                    meshesByTerrain[terrain] = meshes;
                }

                foreach (TreeMesh tree in meshes)
                {
                    builder.AddInstances(tree.Renderer, tree.Mesh, tree.ObjectToWorld);
                }
            }
        }

        private void ForgetDestroyedTerrains()
        {
            forgotten.Clear();
            foreach (KeyValuePair<Terrain, List<TreeMesh>> pair in meshesByTerrain)
            {
                if (pair.Key == null) forgotten.Add(pair.Key);
            }

            foreach (Terrain terrain in forgotten)
            {
                Dispose(meshesByTerrain[terrain]);
                meshesByTerrain.Remove(terrain);
            }
        }

        private static List<TreeMesh> CollectTreeMeshes(Terrain terrain)
        {
            TerrainData data = terrain.terrainData;
            TreePrototype[] prototypes = data.treePrototypes;
            TreeInstance[] instances = data.treeInstances;
            var placements = new List<Matrix4x4>[prototypes.Length];
            for (int i = 0; i < prototypes.Length; i++)
            {
                placements[i] = new List<Matrix4x4>();
            }

            Vector3 origin = terrain.transform.position;
            foreach (TreeInstance tree in instances)
            {
                Vector3 position = origin + Vector3.Scale(tree.position, data.size);
                Quaternion rotation = Quaternion.AngleAxis(tree.rotation * Mathf.Rad2Deg, Vector3.up);
                Vector3 scale = new(tree.widthScale, tree.heightScale, tree.widthScale);
                placements[tree.prototypeIndex].Add(Matrix4x4.TRS(position, rotation, scale));
            }

            var meshes = new List<TreeMesh>();
            for (int i = 0; i < prototypes.Length; i++)
            {
                GameObject prefab = prototypes[i].prefab;
                if (prefab == null || placements[i].Count == 0) continue;

                foreach (Renderer renderer in NearestRenderersOf(prefab))
                {
                    if (renderer == null || !renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;

                    Matrix4x4 inPrefab = prefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                    var objectToWorld = new NativeArray<Matrix4x4>(placements[i].Count, Allocator.Persistent);
                    for (int j = 0; j < placements[i].Count; j++)
                    {
                        objectToWorld[j] = placements[i][j] * inPrefab;
                    }

                    meshes.Add(new TreeMesh(renderer, filter.sharedMesh, objectToWorld));
                }
            }

            Log.Info($"Terrain {terrain.name} traces {instances.Length} trees as {meshes.Count} meshes");
            return meshes;
        }

        private static Renderer[] NearestRenderersOf(GameObject prefab)
        {
            return prefab.TryGetComponent(out LODGroup lodGroup) ? lodGroup.GetLODs()[0].renderers : prefab.GetComponentsInChildren<Renderer>();
        }

        private static void Dispose(List<TreeMesh> meshes)
        {
            foreach (TreeMesh tree in meshes)
            {
                tree.ObjectToWorld.Dispose();
            }
        }

        private readonly struct TreeMesh
        {
            public readonly Renderer Renderer;
            public readonly Mesh Mesh;
            public readonly NativeArray<Matrix4x4> ObjectToWorld;

            public TreeMesh(Renderer renderer, Mesh mesh, NativeArray<Matrix4x4> objectToWorld)
            {
                Renderer = renderer;
                Mesh = mesh;
                ObjectToWorld = objectToWorld;
            }
        }
    }
}
