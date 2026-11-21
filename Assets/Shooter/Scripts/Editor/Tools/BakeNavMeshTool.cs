using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Shooter.Game;
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

        [MenuItem("Tools/Shooter/World/Bake NavMesh")]
        private static void OpenWindow()
        {
            GetWindow<BakeNavMeshTool>("Bake NavMesh");
        }

        private void OnGUI()
        {
            agentBody = (GameObject)EditorGUILayout.ObjectField("Agent body", agentBody, typeof(GameObject), false);
            EditorGUILayout.LabelField("With a body set, the navmesh agent takes the radius, height, step and slope of its CharacterController. " +
                                       "Trees of the terrains are baked as the objects the game places for them, the data lies next to the scene", EditorStyles.wordWrappedMiniLabel);

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
            var placedTrees = new List<Transform>();
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                placedTrees.Add(PlaceTerrainTreesOnStart.PlaceTrees(terrain));
            }

            try
            {
                foreach (NavMeshSurface surface in surfaces)
                {
                    surface.BuildNavMesh();
                }
            }
            finally
            {
                foreach (Transform trees in placedTrees)
                {
                    Object.DestroyImmediate(trees.gameObject);
                }
            }

            foreach (NavMeshSurface surface in surfaces)
            {
                SaveNavMeshDataNextToScene(surface);
                EditorUtility.SetDirty(surface);
            }

            Log.Info($"Baked {surfaces.Length} navmesh surfaces with the trees of the terrains in {stopwatch.ElapsedMilliseconds} ms");
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
    }
}
