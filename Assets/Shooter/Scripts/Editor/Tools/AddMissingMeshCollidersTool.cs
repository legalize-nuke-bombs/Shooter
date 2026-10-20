using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public static class AddMissingMeshCollidersTool
    {
        private const string MenuItemPath = "Tools/Add Missing Mesh Colliders";
        private static readonly Journal Log = Logs.Here();

        [MenuItem(MenuItemPath)]
        private static void AddMissingCollidersToSelectedPrefabs()
        {
            var prefabPaths = FindPrefabPathsInSelection().ToList();
            if (prefabPaths.Count == 0)
            {
                Log.Warn("No prefabs among the selected assets, pick a folder or a prefab in the project window");
                return;
            }

            int changedPrefabs = 0;
            int addedColliders = 0;
            for (int i = 0; i < prefabPaths.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Add Missing Mesh Colliders", prefabPaths[i], (float)i / prefabPaths.Count)) break;

                int addedToPrefab = AddMissingCollidersToPrefab(prefabPaths[i]);
                if (addedToPrefab == 0) continue;

                changedPrefabs++;
                addedColliders += addedToPrefab;
            }

            EditorUtility.ClearProgressBar();

            Log.Info($"Looked at {prefabPaths.Count} prefabs, added {addedColliders} mesh colliders to {changedPrefabs} of them");
        }

        [MenuItem(MenuItemPath, true)]
        private static bool HasSelection()
        {
            return Selection.assetGUIDs.Length > 0;
        }

        private static int AddMissingCollidersToPrefab(string prefabPath)
        {
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                HashSet<Renderer> fartherLodRenderers = FindRenderersOnlyInFartherLods(prefabContents);
                int addedColliders = 0;
                foreach (MeshFilter meshFilter in prefabContents.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (meshFilter.sharedMesh == null) continue;
                    if (meshFilter.GetComponent<Collider>() != null) continue;
                    if (fartherLodRenderers.Contains(meshFilter.GetComponent<Renderer>())) continue;

                    meshFilter.gameObject.AddComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
                    addedColliders++;
                }

                if (addedColliders > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                    Log.Info($"Prefab {prefabPath} got {addedColliders} mesh colliders");
                }

                return addedColliders;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static HashSet<Renderer> FindRenderersOnlyInFartherLods(GameObject prefabContents)
        {
            var nearestLodRenderers = new HashSet<Renderer>();
            var fartherLodRenderers = new HashSet<Renderer>();
            foreach (LODGroup lodGroup in prefabContents.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = lodGroup.GetLODs();
                for (int level = 0; level < lods.Length; level++)
                {
                    foreach (Renderer renderer in lods[level].renderers)
                    {
                        if (renderer == null) continue;

                        if (level == 0)
                        {
                            nearestLodRenderers.Add(renderer);
                        }
                        else
                        {
                            fartherLodRenderers.Add(renderer);
                        }
                    }
                }
            }

            fartherLodRenderers.ExceptWith(nearestLodRenderers);
            return fartherLodRenderers;
        }

        private static IEnumerable<string> FindPrefabPathsInSelection()
        {
            string[] selectedPaths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            return AssetDatabase.FindAssets("t:Prefab", selectedPaths).Select(AssetDatabase.GUIDToAssetPath).Distinct();
        }
    }
}
