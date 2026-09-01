using System.Diagnostics;
using Shooter.Logging;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    public static class NavMeshTreeBaker
    {
        private static readonly Journal Log = Logs.Here();

        [MenuItem("Tools/Bake NavMesh With Trees")]
        public static void Bake()
        {
            NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include);
            if (surfaces.Length == 0)
            {
                Log.Error("No NavMeshSurface in the scene, nothing to bake");
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            GameObject proxies = BuildTreeProxies();

            try
            {
                foreach (NavMeshSurface surface in surfaces) surface.BuildNavMesh();
            }
            finally
            {
                Object.DestroyImmediate(proxies);
            }

            foreach (NavMeshSurface surface in surfaces) EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();

            Log.Info($"Baked {surfaces.Length} navmesh surfaces with tree proxies in {stopwatch.ElapsedMilliseconds} ms");
        }

        private static GameObject BuildTreeProxies()
        {
            var root = new GameObject("TreeBakeProxies");
            int built = 0;

            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                TerrainData data = terrain.terrainData;

                TreePrototype[] prototypes = data.treePrototypes;
                var capsules = new CapsuleCollider[prototypes.Length];
                for (int i = 0; i < prototypes.Length; i++)
                    capsules[i] = prototypes[i].prefab == null
                        ? null
                        : prototypes[i].prefab.GetComponentInChildren<CapsuleCollider>(true);

                foreach (TreeInstance tree in data.treeInstances)
                {
                    CapsuleCollider capsule = capsules[tree.prototypeIndex];
                    if (capsule == null) continue;

                    var proxy = new GameObject("TreeProxy");
                    proxy.transform.SetParent(root.transform, false);
                    proxy.transform.position = terrain.transform.position + Vector3.Scale(tree.position, data.size);

                    var collider = proxy.AddComponent<CapsuleCollider>();
                    collider.radius = capsule.radius * tree.widthScale;
                    collider.height = capsule.height * tree.heightScale;
                    collider.center = Vector3.Scale(capsule.center,
                        new Vector3(tree.widthScale, tree.heightScale, tree.widthScale));

                    built++;
                }
            }

            Log.Info($"Built {built} tree proxies for the bake");
            return root;
        }
    }
}
