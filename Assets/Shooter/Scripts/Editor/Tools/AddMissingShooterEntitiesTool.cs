using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
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
        private EntityTree tree;
        private SearchField searchField;

        [MenuItem("Tools/Shooter/Prefabs/Add Missing Shooter Entities")]
        private static void OpenWindow()
        {
            GetWindow<AddMissingShooterEntitiesTool>("Shooter Entities");
        }

        private void OnGUI()
        {
            skipBareObjects = EditorGUILayout.ToggleLeft("Skip objects with only a Transform", skipBareObjects);
            EditorGUILayout.LabelField("Looks through every prefab under Assets/Shooter and the open scenes. An object is listed where it is defined: a part of one of our prefabs is fixed in that prefab, not in every place it is used. A tick or a mobility on any row goes down to everything under it", EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Find objects without a ShooterEntity"))
            {
                Find();
            }

            if (tree == null) return;

            EditorGUILayout.LabelField($"{found.Count} objects, {found.Count(missing => missing.Checked)} checked", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                searchField ??= new SearchField();
                tree.searchString = searchField.OnToolbarGUI(tree.searchString);
                if (GUILayout.Button("Expand all", EditorStyles.toolbarButton, GUILayout.Width(80))) tree.ExpandAll();
                if (GUILayout.Button("Collapse all", EditorStyles.toolbarButton, GUILayout.Width(80))) tree.CollapseAll();
            }

            tree.OnGUI(GUILayoutUtility.GetRect(0f, float.MaxValue, 0f, float.MaxValue));

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

            tree = new EntityTree(found);
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
                RootIndex = root.GetSiblingIndex(),
                SiblingPath = GetSiblingPath(root, part),
                Names = GetNames(root, part),
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
            var serializedObserver = new SerializedObject(entity.GetComponent<ShooterTransformObserver>());
            serializedObserver.FindProperty(ShooterTransformObserver.MobilityField).enumValueIndex = (int)mobility;
            if (undoable) serializedObserver.ApplyModifiedProperties();
            else serializedObserver.ApplyModifiedPropertiesWithoutUndo();
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

        private static string[] GetNames(Transform root, Transform part)
        {
            var names = new List<string>();
            for (Transform current = part; current != root.parent; current = current.parent)
            {
                names.Insert(0, current.name);
            }

            return names.ToArray();
        }

        private static Transform FindBySiblingPath(Transform root, IEnumerable<int> siblingPath)
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
            public int RootIndex;
            public int[] SiblingPath;
            public string[] Names;
            public string Components;
            public bool Checked = true;
            public ShooterTransformObserver.TransformMobility Mobility;
        }

        private sealed class Node
        {
            public readonly Dictionary<string, Node> ChildrenByKey = new();
            public readonly List<Node> Children = new();
            public string Name;
            public string AssetPath;
            public string PrefabPath;
            public int[] SiblingPath;
            public GameObject SceneObject;
            public MissingEntity Missing;

            public bool IsObject => SiblingPath != null || SceneObject != null;

            public Node GetOrAddChild(string key, string name)
            {
                if (ChildrenByKey.TryGetValue(key, out Node child)) return child;

                child = new Node { Name = name };
                ChildrenByKey[key] = child;
                Children.Add(child);
                return child;
            }

            public IEnumerable<MissingEntity> MissingUnder()
            {
                if (Missing != null) yield return Missing;

                foreach (Node child in Children)
                {
                    foreach (MissingEntity missing in child.MissingUnder())
                    {
                        yield return missing;
                    }
                }
            }

            public Object FindPingTarget()
            {
                if (SceneObject != null) return SceneObject;
                if (PrefabPath == null) return AssetDatabase.LoadAssetAtPath<Object>(AssetPath);

                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                return SiblingPath == null ? prefabRoot : FindBySiblingPath(prefabRoot.transform, SiblingPath).gameObject;
            }
        }

        private sealed class EntityTree : TreeView<int>
        {
            private const int NameColumn = 0;
            private const int ComponentsColumn = 1;
            private const int AddColumn = 2;
            private const int MobilityColumn = 3;

            private readonly Node rootNode;
            private readonly Dictionary<int, Node> nodesById = new();

            public EntityTree(IEnumerable<MissingEntity> found) : base(new TreeViewState<int>(), CreateHeader())
            {
                rootNode = BuildNodes(found);
                showAlternatingRowBackgrounds = true;
                columnIndexForTreeFoldouts = NameColumn;
                Reload();
            }

            protected override TreeViewItem<int> BuildRoot()
            {
                nodesById.Clear();
                var rootItem = new TreeViewItem<int> { id = 0, depth = -1, displayName = "" };
                int nextId = 1;
                AddItems(rootItem, rootNode, 0, ref nextId);
                SetupDepthsFromParentsAndChildren(rootItem);
                return rootItem;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                Node node = nodesById[args.item.id];
                for (int i = 0; i < args.GetNumVisibleColumns(); i++)
                {
                    Rect cell = args.GetCellRect(i);
                    CenterRectUsingSingleLineHeight(ref cell);
                    switch (args.GetColumn(i))
                    {
                        case NameColumn:
                            cell.xMin += GetContentIndent(args.item);
                            using (new EditorGUI.DisabledScope(node.IsObject && node.Missing == null))
                            {
                                EditorGUI.LabelField(cell, args.label);
                            }

                            break;
                        case ComponentsColumn:
                            if (node.Missing != null) EditorGUI.LabelField(cell, new GUIContent(node.Missing.Components, node.Missing.Components), EditorStyles.miniLabel);
                            break;
                        case AddColumn:
                            DrawChecked(cell, node);
                            break;
                        case MobilityColumn:
                            DrawMobility(cell, node);
                            break;
                    }
                }
            }

            protected override void DoubleClickedItem(int id)
            {
                Object target = nodesById[id].FindPingTarget();
                Selection.activeObject = target;
                EditorGUIUtility.PingObject(target);
            }

            private static void DrawChecked(Rect cell, Node node)
            {
                List<MissingEntity> under = node.MissingUnder().ToList();
                bool allChecked = under.All(missing => missing.Checked);
                EditorGUI.showMixedValue = !allChecked && under.Any(missing => missing.Checked);
                EditorGUI.BeginChangeCheck();
                bool isChecked = EditorGUI.Toggle(cell, allChecked);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (MissingEntity missing in under)
                    {
                        missing.Checked = isChecked;
                    }
                }

                EditorGUI.showMixedValue = false;
            }

            private static void DrawMobility(Rect cell, Node node)
            {
                List<MissingEntity> under = node.MissingUnder().ToList();
                ShooterTransformObserver.TransformMobility first = under[0].Mobility;
                EditorGUI.showMixedValue = under.Any(missing => missing.Mobility != first);
                EditorGUI.BeginChangeCheck();
                var mobility = (ShooterTransformObserver.TransformMobility)EditorGUI.EnumPopup(cell, first);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (MissingEntity missing in under)
                    {
                        missing.Mobility = mobility;
                    }
                }

                EditorGUI.showMixedValue = false;
            }

            private void AddItems(TreeViewItem<int> parentItem, Node parentNode, int depth, ref int nextId)
            {
                foreach (Node node in parentNode.Children)
                {
                    var item = new TreeViewItem<int> { id = nextId++, depth = depth, displayName = node.Name };
                    nodesById[item.id] = node;
                    parentItem.AddChild(item);
                    AddItems(item, node, depth + 1, ref nextId);
                }
            }

            private static Node BuildNodes(IEnumerable<MissingEntity> found)
            {
                var root = new Node();
                foreach (MissingEntity missing in found.OrderBy(missing => missing.Location).ThenBy(missing => string.Join("/", missing.SiblingPath.Prepend(missing.RootIndex).Select(index => index.ToString("D4")))))
                {
                    Node node = root;
                    string[] locationParts = missing.Location.Split('/');
                    for (int i = 2; i < locationParts.Length; i++)
                    {
                        string assetPath = string.Join("/", locationParts.Take(i + 1));
                        node = node.GetOrAddChild(assetPath, locationParts[i]);
                        node.AssetPath = assetPath;
                        if (i == locationParts.Length - 1) node.PrefabPath = missing.PrefabPath;
                    }

                    for (int level = 0; level < missing.Names.Length; level++)
                    {
                        int siblingIndex = level == 0 ? missing.RootIndex : missing.SiblingPath[level - 1];
                        node = node.GetOrAddChild($"{siblingIndex}:{missing.Names[level]}", missing.Names[level]);
                        node.PrefabPath = missing.PrefabPath;
                        node.SiblingPath = missing.SiblingPath.Take(level).ToArray();
                        if (missing.SceneObject != null) node.SceneObject = GetAncestor(missing.SceneObject, missing.Names.Length - 1 - level);
                    }

                    node.Missing = missing;
                }

                while (root.Children.Count == 1 && root.Children[0].AssetPath != null && !root.Children[0].AssetPath.Contains('.'))
                {
                    root = root.Children[0];
                }

                return root;
            }

            private static GameObject GetAncestor(GameObject gameObject, int generations)
            {
                Transform current = gameObject.transform;
                for (int i = 0; i < generations; i++)
                {
                    current = current.parent;
                }

                return current.gameObject;
            }

            private static MultiColumnHeader CreateHeader()
            {
                var columns = new[]
                {
                    new MultiColumnHeaderState.Column { headerContent = new GUIContent("Object"), width = 360, minWidth = 120 },
                    new MultiColumnHeaderState.Column { headerContent = new GUIContent("Components"), width = 300, minWidth = 80 },
                    new MultiColumnHeaderState.Column { headerContent = new GUIContent("Add"), width = 36, minWidth = 36, maxWidth = 36, autoResize = false },
                    new MultiColumnHeaderState.Column { headerContent = new GUIContent("Mobility"), width = 80, minWidth = 80, maxWidth = 80, autoResize = false }
                };

                return new MultiColumnHeader(new MultiColumnHeaderState(columns));
            }
        }
    }
}
