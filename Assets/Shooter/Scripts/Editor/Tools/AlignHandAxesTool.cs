using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shooter.Game;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Editor
{
    public class AlignHandAxesTool : EditorWindow
    {
        private const float ToleranceDegrees = 0.5f;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject standard;

        [MenuItem("Tools/Align Hand Axes")]
        private static void OpenWindow()
        {
            GetWindow<AlignHandAxesTool>("Align Hand Axes");
        }

        private void OnGUI()
        {
            standard = (GameObject)EditorGUILayout.ObjectField("Standard skin model", standard, typeof(GameObject), false);
            EditorGUILayout.LabelField("Every other skin model gets its hands turned to the axes of this one", EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUI.DisabledScope(standard == null))
            {
                if (GUILayout.Button("Align hand axes"))
                {
                    AlignEverySkinModelToStandard();
                }
            }
        }

        private void AlignEverySkinModelToStandard()
        {
            string standardPath = AssetDatabase.GetAssetPath(standard);
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                NeutralHands standardHands = NeutralHands.MeasureOnModel(standardPath, previewScene);
                if (standardHands == null)
                {
                    Log.Warn($"Model {standard.name} has no humanoid avatar to be the standard of hand axes");
                    return;
                }

                var reboundMeshes = new HashSet<Mesh>();
                foreach (IGrouping<string, SkinAsset> skinsOfModel in FindSkinsWithModels().GroupBy(GetModelPathOfSkin))
                {
                    if (skinsOfModel.Key != null && skinsOfModel.Key != standardPath)
                    {
                        AlignModelToStandard(skinsOfModel.Key, skinsOfModel, standardHands, previewScene, reboundMeshes);
                    }
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void AlignModelToStandard(string modelPath, IEnumerable<SkinAsset> skins, NeutralHands standardHands, Scene previewScene, HashSet<Mesh> reboundMeshes)
        {
            if (AssetImporter.GetAtPath(modelPath) is not ModelImporter importer) return;

            NeutralHands modelHands = NeutralHands.MeasureOnModel(modelPath, previewScene);
            if (modelHands == null)
            {
                Log.Warn($"Skin model {modelPath} has no humanoid avatar, its hands stay as they are");
                return;
            }

            Quaternion rightTurn = Quaternion.Inverse(modelHands.RightRotation) * standardHands.RightRotation;
            Quaternion leftTurn = Quaternion.Inverse(modelHands.LeftRotation) * standardHands.LeftRotation;
            if (GetTurnAngle(rightTurn) < ToleranceDegrees && GetTurnAngle(leftTurn) < ToleranceDegrees) return;

            HandAxisPostprocessor.HandTurns handTurns = HandAxisPostprocessor.HandTurns.ReadFromUserData(importer.userData) ?? new HandAxisPostprocessor.HandTurns();
            handTurns.AddTurn(modelHands.RightBoneName, rightTurn);
            handTurns.AddTurn(modelHands.LeftBoneName, leftTurn);
            importer.userData = JsonUtility.ToJson(handTurns);
            importer.humanDescription = TurnHandsInHumanDescription(importer.humanDescription, modelHands, rightTurn, leftTurn);
            importer.SaveAndReimport();

            foreach (SkinAsset skin in skins)
            {
                TurnUnpackedSkin(skin, modelPath, rightTurn, leftTurn, reboundMeshes);
            }

            Log.Info($"Skin model {Path.GetFileNameWithoutExtension(modelPath)} turned its hands to the standard axes: right by {GetTurnAngle(rightTurn):F0}°, left by {GetTurnAngle(leftTurn):F0}°");
        }

        private static void TurnUnpackedSkin(SkinAsset skin, string modelPath, Quaternion rightTurn, Quaternion leftTurn, HashSet<Mesh> reboundMeshes)
        {
            string skinPrefabPath = AssetDatabase.GetAssetPath(skin.Model);
            if (skinPrefabPath == modelPath) return;

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(skinPrefabPath);
            try
            {
                Animator animator = prefabContents.GetComponent<Animator>();
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

                bool hasOwnBones = PrefabUtility.GetCorrespondingObjectFromOriginalSource(rightHand) == null;
                if (hasOwnBones)
                {
                    HandAxisPostprocessor.TurnHandKeepingChildrenInPlace(rightHand, rightTurn);
                    HandAxisPostprocessor.TurnHandKeepingChildrenInPlace(leftHand, leftTurn);
                }

                foreach (SkinnedMeshRenderer skinnedRenderer in prefabContents.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (skinnedRenderer.sharedMesh == null || AssetDatabase.GetAssetPath(skinnedRenderer.sharedMesh) == modelPath) continue;
                    if (!reboundMeshes.Add(skinnedRenderer.sharedMesh)) continue;

                    bool isReboundToRightHand = HandAxisPostprocessor.TryRebindMeshToTurnedHand(skinnedRenderer.sharedMesh, skinnedRenderer.bones, rightHand, rightTurn);
                    bool isReboundToLeftHand = HandAxisPostprocessor.TryRebindMeshToTurnedHand(skinnedRenderer.sharedMesh, skinnedRenderer.bones, leftHand, leftTurn);
                    if (!isReboundToRightHand && !isReboundToLeftHand) continue;

                    EditorUtility.SetDirty(skinnedRenderer.sharedMesh);
                    AssetDatabase.SaveAssetIfDirty(skinnedRenderer.sharedMesh);
                    Log.Info($"Mesh {AssetDatabase.GetAssetPath(skinnedRenderer.sharedMesh)} of skin {skin.name} rebound to the turned hands");
                }

                if (hasOwnBones)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, skinPrefabPath);
                    Log.Info($"Skin {skin.name} turned the hands of its own bones copy in {skinPrefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static HumanDescription TurnHandsInHumanDescription(HumanDescription description, NeutralHands hands, Quaternion rightTurn, Quaternion leftTurn)
        {
            SkeletonBone[] skeleton = description.skeleton;
            for (int i = 0; i < skeleton.Length; i++)
            {
                SkeletonBone bone = skeleton[i];
                if (bone.name == hands.RightBoneName)
                {
                    bone.rotation *= rightTurn;
                }
                else if (bone.name == hands.LeftBoneName)
                {
                    bone.rotation *= leftTurn;
                }
                else if (hands.RightChildNames.Contains(bone.name))
                {
                    UndoParentTurn(ref bone, rightTurn);
                }
                else if (hands.LeftChildNames.Contains(bone.name))
                {
                    UndoParentTurn(ref bone, leftTurn);
                }

                skeleton[i] = bone;
            }

            description.skeleton = skeleton;
            return description;
        }

        private static void UndoParentTurn(ref SkeletonBone bone, Quaternion parentTurn)
        {
            Quaternion inverseTurn = Quaternion.Inverse(parentTurn);
            bone.position = inverseTurn * bone.position;
            bone.rotation = inverseTurn * bone.rotation;
        }

        private static IEnumerable<SkinAsset> FindSkinsWithModels()
        {
            return AssetDatabase.FindAssets("t:" + nameof(SkinAsset))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkinAsset>)
                .Where(skin => skin != null && skin.Model != null);
        }

        private static string GetModelPathOfSkin(SkinAsset skin)
        {
            Animator animator = skin.Model.GetComponent<Animator>();
            return animator == null || animator.avatar == null ? null : AssetDatabase.GetAssetPath(animator.avatar);
        }

        private static float GetTurnAngle(Quaternion turn)
        {
            return Quaternion.Angle(Quaternion.identity, turn);
        }

        private class NeutralHands
        {
            public Quaternion RightRotation { get; private set; }
            public Quaternion LeftRotation { get; private set; }
            public string RightBoneName { get; private set; }
            public string LeftBoneName { get; private set; }
            public HashSet<string> RightChildNames { get; private set; }
            public HashSet<string> LeftChildNames { get; private set; }

            public static NeutralHands MeasureOnModel(string modelPath, Scene previewScene)
            {
                GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (modelAsset == null) return null;

                GameObject modelInstance = Object.Instantiate(modelAsset);
                SceneManager.MoveGameObjectToScene(modelInstance, previewScene);
                try
                {
                    modelInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    Animator animator = modelInstance.GetComponent<Animator>();
                    if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return null;

                    PutInNeutralPose(animator);
                    Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    return new NeutralHands
                    {
                        RightRotation = rightHand.rotation,
                        LeftRotation = leftHand.rotation,
                        RightBoneName = rightHand.name,
                        LeftBoneName = leftHand.name,
                        RightChildNames = GetChildNames(rightHand),
                        LeftChildNames = GetChildNames(leftHand)
                    };
                }
                finally
                {
                    Object.DestroyImmediate(modelInstance);
                }
            }

            private static void PutInNeutralPose(Animator animator)
            {
                using HumanPoseHandler poseHandler = new(animator.avatar, animator.transform);
                HumanPose pose = new();
                poseHandler.GetHumanPose(ref pose);
                pose.bodyRotation = Quaternion.identity;
                pose.muscles = new float[HumanTrait.MuscleCount];
                poseHandler.SetHumanPose(ref pose);
            }

            private static HashSet<string> GetChildNames(Transform hand)
            {
                var names = new HashSet<string>();
                foreach (Transform child in hand)
                {
                    names.Add(child.name);
                }

                return names;
            }
        }
    }
}
