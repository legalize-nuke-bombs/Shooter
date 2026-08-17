using System.Collections.Generic;
using System.Linq;
using Shooter.Logging;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    public static class ColliderFiller
    {
        private const string Menu = "Tools/Fill Missing Colliders";
        private static readonly Journal Log = Logs.Here();

        [MenuItem(Menu)]
        private static void Fill()
        {
            var paths = Prefabs().ToList();
            if (paths.Count == 0)
            {
                Log.Warn("Nothing selected in the project window, pick a folder or a prefab");
                return;
            }

            int dressed = 0;
            int added = 0;

            for (int i = 0; i < paths.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar(Menu, paths[i], (float)i / paths.Count))
                    break;

                int grown = Dress(paths[i]);
                if (grown <= 0) continue;

                dressed++;
                added += grown;
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();

            Log.Warn($"Looked at {paths.Count} prefabs, dressed {dressed} of them with {added} colliders");
        }

        [MenuItem(Menu, true)]
        private static bool Ready()
        {
            return Selection.assetGUIDs.Length > 0;
        }

        private static int Dress(string path)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            int added = 0;

            try
            {
                foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null) continue;
                    if (filter.GetComponent<Collider>() != null) continue;

                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                    added++;
                }

                if (added > 0) PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            if (added > 0) Log.Info($"Prefab {path} got {added} colliders");

            return added;
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
    }
}
