using System.Diagnostics;
using System.IO;
using Shooter.Logging;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Editing
{
    public static class NavMeshTreeBaker
    {
        private const string CharacterPrefabPath = "Assets/Game/Body/Data/Character.prefab";
        private const string AgentSettingsPath = "ProjectSettings/NavMeshAreas.asset";
        private const float SlopeForgiveness = 5f;
        private static readonly Journal Log = Logs.Here();

        [MenuItem("Tools/Bake NavMesh With Trees")]
        public static void Bake()
        {
            if (EditorApplication.isPlaying)
            {
                Log.Error("Navmesh is baked in edit mode only, stop the play mode first");
                return;
            }

            NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include);
            if (surfaces.Length == 0)
            {
                Log.Error("No NavMeshSurface in the scene, nothing to bake");
                return;
            }

            if (!SyncAgentToBody()) return;

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

            foreach (NavMeshSurface surface in surfaces)
            {
                Persist(surface);
                EditorUtility.SetDirty(surface);
            }

            AssetDatabase.SaveAssets();

            Log.Info($"Baked {surfaces.Length} navmesh surfaces with tree proxies in {stopwatch.ElapsedMilliseconds} ms");
        }

        private static bool SyncAgentToBody()
        {
            var character = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (character == null)
            {
                Log.Error($"Character prefab not found at {CharacterPrefabPath}, navmesh agent cannot follow the body");
                return false;
            }

            var controller = character.GetComponent<CharacterController>();
            if (controller == null)
            {
                Log.Error($"Prefab {character.name} carries no CharacterController, navmesh agent cannot follow the body");
                return false;
            }

            var serialized = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(AgentSettingsPath)[0]);
            SerializedProperty agent = serialized.FindProperty("m_Settings").GetArrayElementAtIndex(0);
            agent.FindPropertyRelative("agentRadius").floatValue = controller.radius;
            agent.FindPropertyRelative("agentHeight").floatValue = controller.height;
            agent.FindPropertyRelative("agentClimb").floatValue = controller.stepOffset;
            agent.FindPropertyRelative("agentSlope").floatValue = controller.slopeLimit - SlopeForgiveness;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Log.Info($"Navmesh agent follows {character.name}: radius {controller.radius}, height {controller.height}, climb {controller.stepOffset}, slope {controller.slopeLimit - SlopeForgiveness}");
            return true;
        }

        private static void Persist(NavMeshSurface surface)
        {
            NavMeshData data = surface.navMeshData;
            if (data == null || EditorUtility.IsPersistent(data)) return;

            string scenePath = surface.gameObject.scene.path;
            string sceneDirectory = Path.GetDirectoryName(scenePath);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            string folder = $"{sceneDirectory}/{sceneName}";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(sceneDirectory, sceneName);

            string path = $"{folder}/NavMesh-{surface.name}.asset";
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
                    colliders[i] = prototypes[i].prefab == null
                        ? new Collider[0]
                        : prototypes[i].prefab.GetComponentsInChildren<Collider>(true);

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

            Log.Info($"Built {built} tree proxies carrying {shapes} colliders for the bake");
            return root;
        }

        private static Vector3 Relative(Vector3 scale, Vector3 root)
        {
            return new Vector3(scale.x / root.x, scale.y / root.y, scale.z / root.z);
        }
    }
}
