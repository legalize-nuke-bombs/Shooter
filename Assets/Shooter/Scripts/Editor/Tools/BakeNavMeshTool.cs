using System.Diagnostics;
using System.IO;
using Shooter.Infrastructure;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Editor
{
    public class BakeNavMeshTool : EditorWindow
    {
        private const string AgentSettingsPath = "ProjectSettings/NavMeshAreas.asset";
        private const float SlopeForgiveness = 5f;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject agentBody;

        [MenuItem("Tools/Bake NavMesh")]
        private static void Open()
        {
            GetWindow<BakeNavMeshTool>("Bake NavMesh");
        }

        private void OnGUI()
        {
            agentBody = (GameObject)EditorGUILayout.ObjectField("Agent body", agentBody, typeof(GameObject), false);
            EditorGUILayout.LabelField("With a body set, the navmesh agent takes the radius, height, step and slope of its CharacterController. " +
                                       "Trees of the terrains are baked through temporary copies of their colliders, the data lies next to the scene",
                EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Bake"))
            {
                Bake();
            }
        }

        private void Bake()
        {
            if (EditorApplication.isPlaying)
            {
                Log.Warn("Navmesh is baked in edit mode only, stop the play mode first");
                return;
            }

            NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include);
            if (surfaces.Length == 0)
            {
                Log.Warn("No NavMeshSurface in the scene, nothing to bake");
                return;
            }

            if (agentBody != null && !SyncAgentTo(agentBody)) return;

            Stopwatch stopwatch = Stopwatch.StartNew();
            GameObject proxies = BuildTreeProxies();
            try
            {
                foreach (NavMeshSurface surface in surfaces)
                {
                    surface.BuildNavMesh();
                }
            }
            finally
            {
                Object.DestroyImmediate(proxies);
            }

            foreach (NavMeshSurface surface in surfaces)
            {
                SaveNextToScene(surface);
                EditorUtility.SetDirty(surface);
            }

            AssetDatabase.SaveAssets();
            Log.Info($"Baked {surfaces.Length} navmesh surfaces with tree proxies in {stopwatch.ElapsedMilliseconds} ms");
        }

        private static bool SyncAgentTo(GameObject body)
        {
            var controller = body.GetComponent<CharacterController>();
            if (controller == null)
            {
                Log.Warn($"Agent body {body.name} carries no CharacterController, the navmesh agent cannot follow it");
                return false;
            }

            var serialized = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(AgentSettingsPath)[0]);
            SerializedProperty agent = serialized.FindProperty("m_Settings").GetArrayElementAtIndex(0);
            agent.FindPropertyRelative("agentRadius").floatValue = controller.radius;
            agent.FindPropertyRelative("agentHeight").floatValue = controller.height;
            agent.FindPropertyRelative("agentClimb").floatValue = controller.stepOffset;
            agent.FindPropertyRelative("agentSlope").floatValue = controller.slopeLimit - SlopeForgiveness;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Log.Debug($"Navmesh agent follows {body.name}: radius {controller.radius}, height {controller.height}, climb {controller.stepOffset}, slope {controller.slopeLimit - SlopeForgiveness}");
            return true;
        }

        private static void SaveNextToScene(NavMeshSurface surface)
        {
            NavMeshData data = surface.navMeshData;
            if (data == null || EditorUtility.IsPersistent(data)) return;

            string path = $"{Path.GetDirectoryName(surface.gameObject.scene.path)}/NavMesh-{surface.name}.asset";
            data.name = $"NavMesh-{surface.name}";
            AssetDatabase.CreateAsset(data, path);
            EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);

            Log.Info($"Navmesh data of {surface.name} saved to {path}");
        }

        private static GameObject BuildTreeProxies()
        {
            var root = new GameObject("TreeBakeProxies");
            int built = 0;
            int shapes = 0;

            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                TerrainData data = terrain.terrainData;
                TreePrototype[] prototypes = data.treePrototypes;
                var colliders = new Collider[prototypes.Length][];
                for (int i = 0; i < prototypes.Length; i++)
                {
                    colliders[i] = prototypes[i].prefab == null ? new Collider[0] : prototypes[i].prefab.GetComponentsInChildren<Collider>(true);
                }

                foreach (TreeInstance tree in data.treeInstances)
                {
                    if (colliders[tree.prototypeIndex].Length == 0) continue;

                    Transform proxy = new GameObject("TreeProxy").transform;
                    proxy.SetParent(root.transform, false);
                    proxy.SetPositionAndRotation(
                        terrain.transform.position + Vector3.Scale(tree.position, data.size),
                        Quaternion.Euler(0f, tree.rotation * Mathf.Rad2Deg, 0f));
                    proxy.localScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);

                    Transform prefab = prototypes[tree.prototypeIndex].prefab.transform;
                    foreach (Collider collider in colliders[tree.prototypeIndex])
                    {
                        if (!collider.enabled) continue;

                        Transform part = new GameObject(collider.name).transform;
                        part.SetParent(proxy, false);
                        part.localPosition = prefab.InverseTransformPoint(collider.transform.position);
                        part.localRotation = Quaternion.Inverse(prefab.rotation) * collider.transform.rotation;
                        part.localScale = Relative(collider.transform.lossyScale, prefab.lossyScale);
                        EditorUtility.CopySerialized(collider, part.gameObject.AddComponent(collider.GetType()));
                        shapes++;
                    }

                    built++;
                }
            }

            Log.Debug($"Built {built} tree proxies carrying {shapes} colliders for the bake");
            return root;
        }

        private static Vector3 Relative(Vector3 scale, Vector3 root)
        {
            return new Vector3(scale.x / root.x, scale.y / root.y, scale.z / root.z);
        }
    }
}
