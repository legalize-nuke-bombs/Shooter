using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public static class AddMissingMeshCollidersMenuItem
    {
        private const string MenuItemPath = "Tools/Add Missing Mesh Colliders";
        private static readonly Journal Log = Logs.Here();

        [MenuItem(MenuItemPath)]
        private static void AddToSelectedPrefabs()
        {
            var paths = SelectedPrefabPaths().ToList();
            if (paths.Count == 0)
            {
                Log.Warn("No prefabs among the selected assets, pick a folder or a prefab in the project window");
                return;
            }

            int changedPrefabs = 0;
            int addedColliders = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Add Missing Mesh Colliders", paths[i], (float)i / paths.Count)) break;

                int addedToPrefab = AddToPrefab(paths[i]);
                if (addedToPrefab == 0) continue;

                changedPrefabs++;
                addedColliders += addedToPrefab;
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();

            Log.Info($"Looked at {paths.Count} prefabs, added {addedColliders} mesh colliders to {changedPrefabs} of them");
        }

        [MenuItem(MenuItemPath, true)]
        private static bool HasSelection()
        {
            return Selection.assetGUIDs.Length > 0;
        }

        private static int AddToPrefab(string path)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                HashSet<Renderer> fartherLods = RenderersOnlyInFartherLods(prefab);
                int added = 0;
                foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null) continue;
                    if (filter.GetComponent<Collider>() != null) continue;
                    if (fartherLods.Contains(filter.GetComponent<Renderer>())) continue;

                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                    added++;
                }

                if (added > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    Log.Debug($"Prefab {path} got {added} mesh colliders");
                }

                return added;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static HashSet<Renderer> RenderersOnlyInFartherLods(GameObject prefab)
        {
            var nearestLod = new HashSet<Renderer>();
            var fartherLods = new HashSet<Renderer>();
            foreach (LODGroup group in prefab.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = group.GetLODs();
                for (int level = 0; level < lods.Length; level++)
                {
                    foreach (Renderer renderer in lods[level].renderers)
                    {
                        if (renderer == null) continue;

                        if (level == 0)
                        {
                            nearestLod.Add(renderer);
                        }
                        else
                        {
                            fartherLods.Add(renderer);
                        }
                    }
                }
            }

            fartherLods.ExceptWith(nearestLod);
            return fartherLods;
        }

        private static IEnumerable<string> SelectedPrefabPaths()
        {
            string[] selected = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            return AssetDatabase.FindAssets("t:Prefab", selected).Select(AssetDatabase.GUIDToAssetPath).Distinct();
        }
    }
}
