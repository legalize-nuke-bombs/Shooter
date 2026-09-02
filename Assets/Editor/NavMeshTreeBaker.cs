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
