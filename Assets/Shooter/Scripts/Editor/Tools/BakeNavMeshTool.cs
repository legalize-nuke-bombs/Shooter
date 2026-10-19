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
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private GameObject agentBody;

        [MenuItem("Tools/Bake NavMesh")]
        private static void OpenWindow()
        {
            GetWindow<BakeNavMeshTool>("Bake NavMesh");
        }

        private void OnGUI()
        {
            agentBody = (GameObject)EditorGUILayout.ObjectField("Agent body", agentBody, typeof(GameObject), false);
            EditorGUILayout.LabelField("With a body set, the navmesh agent takes the radius, height, step and slope of its CharacterController. " +
                                       "Trees of the terrains are baked through temporary copies of their colliders, the data lies next to the scene", EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Bake"))
            {
                BakeEveryNavMeshSurface();
            }
        }

        private void BakeEveryNavMeshSurface()
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

            if (agentBody != null && !TrySyncAgentToBody(agentBody)) return;

            Stopwatch stopwatch = Stopwatch.StartNew();
            GameObject treeProxiesRoot = BuildTreeProxies();
            try
            {
                foreach (NavMeshSurface surface in surfaces)
                {
                    surface.BuildNavMesh();
                }
            }
            finally
            {
                Object.DestroyImmediate(treeProxiesRoot);
            }

            foreach (NavMeshSurface surface in surfaces)
            {
                SaveNavMeshDataNextToScene(surface);
                EditorUtility.SetDirty(surface);
            }

            AssetDatabase.SaveAssets();
            Log.Info($"Baked {surfaces.Length} navmesh surfaces with tree proxies in {stopwatch.ElapsedMilliseconds} ms");
        }

        private static bool TrySyncAgentToBody(GameObject body)
        {
            CharacterController characterController = body.GetComponent<CharacterController>();
            if (characterController == null)
            {
                Log.Warn($"Agent body {body.name} carries no CharacterController, the navmesh agent cannot follow it");
                return false;
            }

            SerializedObject serializedNavMeshAreas = new(AssetDatabase.LoadAllAssetsAtPath(AgentSettingsPath)[0]);
            SerializedProperty agentSettings = serializedNavMeshAreas.FindProperty("m_Settings").GetArrayElementAtIndex(0);
            agentSettings.FindPropertyRelative("agentRadius").floatValue = characterController.radius;
            agentSettings.FindPropertyRelative("agentHeight").floatValue = characterController.height;
            agentSettings.FindPropertyRelative("agentClimb").floatValue = characterController.stepOffset;
            agentSettings.FindPropertyRelative("agentSlope").floatValue = characterController.slopeLimit - SlopeForgiveness;
            serializedNavMeshAreas.ApplyModifiedPropertiesWithoutUndo();

            Log.Info($"Navmesh agent follows {body.name}: radius {characterController.radius}, height {characterController.height}, climb {characterController.stepOffset}, slope {characterController.slopeLimit - SlopeForgiveness}");
            return true;
        }

        private static void SaveNavMeshDataNextToScene(NavMeshSurface surface)
        {
            NavMeshData navMeshData = surface.navMeshData;
            if (navMeshData == null || EditorUtility.IsPersistent(navMeshData)) return;

            string path = $"{Path.GetDirectoryName(surface.gameObject.scene.path)}/NavMesh-{surface.name}.asset";
            navMeshData.name = $"NavMesh-{surface.name}";
            AssetDatabase.CreateAsset(navMeshData, path);
            EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);

            Log.Info($"Navmesh data of {surface.name} saved to {path}");
        }

        private static GameObject BuildTreeProxies()
        {
            GameObject treeProxiesRoot = new("TreeBakeProxies");
            int builtProxies = 0;
            int copiedColliders = 0;

            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                TerrainData terrainData = terrain.terrainData;
                TreePrototype[] prototypes = terrainData.treePrototypes;
                Collider[][] collidersOfPrototype = new Collider[prototypes.Length][];
                for (int i = 0; i < prototypes.Length; i++)
                {
                    collidersOfPrototype[i] = prototypes[i].prefab == null ? new Collider[0] : prototypes[i].prefab.GetComponentsInChildren<Collider>(true);
                }

                foreach (TreeInstance tree in terrainData.treeInstances)
                {
                    if (collidersOfPrototype[tree.prototypeIndex].Length == 0) continue;

                    Transform proxy = new GameObject("TreeProxy").transform;
                    proxy.SetParent(treeProxiesRoot.transform, false);
                    proxy.SetPositionAndRotation(terrain.transform.position + Vector3.Scale(tree.position, terrainData.size), Quaternion.Euler(0f, tree.rotation * Mathf.Rad2Deg, 0f));
                    proxy.localScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);

                    Transform prefabRoot = prototypes[tree.prototypeIndex].prefab.transform;
                    foreach (Collider collider in collidersOfPrototype[tree.prototypeIndex])
                    {
                        if (!collider.enabled) continue;

                        Transform part = new GameObject(collider.name).transform;
                        part.SetParent(proxy, false);
                        part.localPosition = prefabRoot.InverseTransformPoint(collider.transform.position);
                        part.localRotation = Quaternion.Inverse(prefabRoot.rotation) * collider.transform.rotation;
                        part.localScale = GetScaleRelativeTo(collider.transform.lossyScale, prefabRoot.lossyScale);
                        EditorUtility.CopySerialized(collider, part.gameObject.AddComponent(collider.GetType()));
                        copiedColliders++;
                    }

                    builtProxies++;
                }
            }

            Log.Info($"Built {builtProxies} tree proxies carrying {copiedColliders} colliders for the bake");
            return treeProxiesRoot;
        }

        private static Vector3 GetScaleRelativeTo(Vector3 scale, Vector3 rootScale)
        {
            return new Vector3(scale.x / rootScale.x, scale.y / rootScale.y, scale.z / rootScale.z);
        }
    }
}
