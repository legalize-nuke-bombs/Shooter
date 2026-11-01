using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Editor
{
    public class AddMissingShooterEntitiesTool : EditorWindow
    {
        private const string OurAssetsFolder = "Assets/Shooter/";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private bool skipBareObjects = true;

        private readonly List<MissingEntity> found = new();
        private Vector2 scrollPosition;

        [MenuItem("Tools/Add Missing Shooter Entities")]
        private static void OpenWindow()
        {
            GetWindow<AddMissingShooterEntitiesTool>("Shooter Entities");
        }

        private void OnGUI()
        {
            skipBareObjects = EditorGUILayout.Toggle("Skip objects with only a Transform", skipBareObjects);
            EditorGUILayout.LabelField("Looks through every prefab under Assets/Shooter and the open scenes. An object is listed where it is defined: a part of one of our prefabs is fixed in that prefab, not in every place it is used", EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Find objects without a ShooterEntity"))
            {
                Find();
            }

            if (found.Count == 0) return;

            EditorGUILayout.LabelField($"{found.Count} objects, {found.Count(missing => missing.Checked)} checked", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            string location = null;
            foreach (MissingEntity missing in found)
            {
                if (missing.Location != location)
                {
                    location = missing.Location;
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(location, EditorStyles.boldLabel);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    missing.Checked = EditorGUILayout.Toggle(missing.Checked, GUILayout.Width(16));
                    EditorGUILayout.LabelField(new GUIContent($"{missing.ObjectPath}   {missing.Components}", missing.Components));
                    missing.Mobility = (ShooterTransformObserver.TransformMobility)EditorGUILayout.EnumPopup(missing.Mobility, GUILayout.Width(70));
                    if (GUILayout.Button("Ping", GUILayout.Width(40))) Ping(missing);
                }
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Add ShooterEntity to the checked objects"))
            {
                AddToChecked();
                Find();
            }
        }

        private void Find()
        {
            found.Clear();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { OurAssetsFolder.TrimEnd('/') });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (EditorUtility.DisplayCancelableProgressBar("Find objects without a ShooterEntity", prefabPath, (float)i / prefabGuids.Length)) break;

                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabRoot == null) continue;

                foreach (Transform part in prefabRoot.GetComponentsInChildren<Transform>(true))
                {
                    TryAddFound(prefabPath, prefabPath, null, prefabRoot.transform, part);
                }
            }

            EditorUtility.ClearProgressBar();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                {
                    foreach (Transform part in sceneRoot.GetComponentsInChildren<Transform>(true))
                    {
                        TryAddFound(scene.path, null, part.gameObject, sceneRoot.transform, part);
                    }
                }
            }

            Log.Info($"Found {found.Count} objects without a ShooterEntity in {found.Select(missing => missing.Location).Distinct().Count()} prefabs and scenes");
        }

        private void TryAddFound(string location, string prefabPath, GameObject sceneObject, Transform root, Transform part)
        {
            Component[] components = part.GetComponents<Component>();
            if (components.Any(component => component is ShooterEntity)) return;
            if (skipBareObjects && components.Length == 1) return;
            if (!IsDefinedHere(part.gameObject)) return;

            found.Add(new MissingEntity
            {
                Location = location,
                PrefabPath = prefabPath,
                SceneObject = sceneObject,
                SiblingPath = GetSiblingPath(root, part),
                ObjectPath = GetObjectPath(root, part),
                Components = string.Join(", ", components.Where(component => component is not Transform).Select(component => component == null ? "Missing script" : component.GetType().Name))
            });
        }

        // An object inside an instance of one of our prefabs belongs to that prefab, which is fixed on its own; models
        // and prefabs of the packs can't take our components, so their objects are fixed where they are placed
        private static bool IsDefinedHere(GameObject gameObject)
        {
            GameObject outermostInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            if (outermostInstanceRoot == null) return true;

            string sourcePath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(outermostInstanceRoot));
            return !sourcePath.StartsWith(OurAssetsFolder) || AssetImporter.GetAtPath(sourcePath) is ModelImporter;
        }

        private void AddToChecked()
        {
            var checkedEntities = found.Where(missing => missing.Checked).ToList();
            foreach (IGrouping<string, MissingEntity> prefabEntities in checkedEntities.Where(missing => missing.PrefabPath != null).GroupBy(missing => missing.PrefabPath))
            {
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabEntities.Key);
                try
                {
                    foreach (MissingEntity missing in prefabEntities)
                    {
                        AddEntity(FindBySiblingPath(prefabContents.transform, missing.SiblingPath).gameObject, missing.Mobility, false);
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabEntities.Key);
                    Log.Info($"Prefab {prefabEntities.Key} got {prefabEntities.Count()} entities");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }

            foreach (IGrouping<string, MissingEntity> sceneEntities in checkedEntities.Where(missing => missing.SceneObject != null).GroupBy(missing => missing.Location))
            {
                foreach (MissingEntity missing in sceneEntities)
                {
                    AddEntity(missing.SceneObject, missing.Mobility, true);
                }

                EditorSceneManager.MarkSceneDirty(sceneEntities.First().SceneObject.scene);
                Log.Info($"Scene {sceneEntities.Key} got {sceneEntities.Count()} entities, it is not saved");
            }
        }

        private static void AddEntity(GameObject gameObject, ShooterTransformObserver.TransformMobility mobility, bool undoable)
        {
            if (gameObject.GetComponent<ShooterEntity>() != null) return;

            ShooterEntity entity = undoable ? Undo.AddComponent<ShooterEntity>(gameObject) : gameObject.AddComponent<ShooterEntity>();
            var serializedEntity = new SerializedObject(entity);
            serializedEntity.FindProperty(ShooterEntity.MobilityField).enumValueIndex = (int)mobility;
            if (undoable) serializedEntity.ApplyModifiedProperties();
            else serializedEntity.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Ping(MissingEntity missing)
        {
            Object target = missing.SceneObject;
            if (target == null)
            {
                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(missing.PrefabPath);
                target = FindBySiblingPath(prefabRoot.transform, missing.SiblingPath).gameObject;
            }

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        private static int[] GetSiblingPath(Transform root, Transform part)
        {
            var siblingPath = new List<int>();
            for (Transform current = part; current != root; current = current.parent)
            {
                siblingPath.Insert(0, current.GetSiblingIndex());
            }

            return siblingPath.ToArray();
        }

        private static string GetObjectPath(Transform root, Transform part)
        {
            var names = new List<string>();
            for (Transform current = part; current != null && current != root.parent; current = current.parent)
            {
                names.Insert(0, current.name);
            }

            return string.Join("/", names);
        }

        private static Transform FindBySiblingPath(Transform root, int[] siblingPath)
        {
            Transform current = root;
            foreach (int siblingIndex in siblingPath)
            {
                current = current.GetChild(siblingIndex);
            }

            return current;
        }

        private sealed class MissingEntity
        {
            public string Location;
            public string PrefabPath;
            public GameObject SceneObject;
            public int[] SiblingPath;
            public string ObjectPath;
            public string Components;
            public bool Checked = true;
            public ShooterTransformObserver.TransformMobility Mobility;
        }
    }
}
