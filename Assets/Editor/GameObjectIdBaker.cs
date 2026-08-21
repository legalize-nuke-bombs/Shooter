using System;
using System.Collections.Generic;
using Shooter.Game.Core.GameObject;
using Shooter.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Editor
{
    public static class GameObjectIdBaker
    {
        private static readonly Journal Log = Logs.Here();

        [MenuItem("Tools/Bake Scene Object IDs")]
        public static void UpdateSceneIds()
        {
            Scene scene = SceneManager.GetActiveScene();
            Log.Info($"Starting baking {scene.name}...");

            GameObjectId[] components = UnityEngine.Object.FindObjectsByType<GameObjectId>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<GameObject> modified = BakeIds(components);
            List<GameObject> duplicates = FindDuplicates(components);

            if (modified.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (duplicates.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "GameObject ID Baker Warning",
                    @$"
Baked {modified.Count} / {components.Length} IDs
Found {duplicates.Count} duplicates!
                            ",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "GameObject ID Baker Done",
                    @$"
Baked {modified.Count} / {components.Length} IDs
                            ",
                    "OK"
                );
            }
        }

        private static List<GameObject> BakeIds(GameObjectId[] components)
        {
            var modified = new List<GameObject>();

            foreach (GameObjectId component in components)
            {
                if (string.IsNullOrEmpty(component.Id))
                {
                    var serializedComponent = new SerializedObject(component);
                    SerializedProperty idProperty = serializedComponent.FindProperty("id");

                    string newGuid = Guid.NewGuid().ToString();
                    idProperty.stringValue = newGuid;

                    serializedComponent.ApplyModifiedProperties();

                    Log.Info($"New ID for [{component.name}]: {newGuid}");
                    modified.Add(component.gameObject);
                }
            }

            return modified;
        }

        private static List<GameObject> FindDuplicates(GameObjectId[] components)
        {
            var usedIds = new HashSet<string>();
            var duplicates = new List<GameObject>();

            foreach (GameObjectId component in components)
            {
                string currentId = component.Id;

                if (!string.IsNullOrEmpty(currentId))
                {
                    if (!usedIds.Add(currentId))
                    {
                        Log.Warn($"Duplicate {component.gameObject} detected!");
                        duplicates.Add(component.gameObject);
                    }
                }
            }

            return duplicates;
        }
    }
}
