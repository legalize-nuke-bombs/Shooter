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
        private const float Tolerance = 0.5f;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject standard;

        [MenuItem("Tools/Align Hand Axes")]
        private static void Open()
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
                    Align();
                }
            }
        }

        private void Align()
        {
            string standardPath = AssetDatabase.GetAssetPath(standard);
            Scene stage = EditorSceneManager.NewPreviewScene();
            try
            {
                NeutralHands standardHands = NeutralHands.Of(standardPath, stage);
                if (standardHands == null)
                {
                    Log.Warn($"Model {standard.name} has no humanoid avatar to be the standard of hand axes");
                    return;
                }

                var rebound = new HashSet<Mesh>();
                foreach (IGrouping<string, SkinSpec> worn in Skins().GroupBy(ModelPathOf))
                {
                    if (worn.Key != null && worn.Key != standardPath)
                    {
                        AlignModel(worn.Key, worn, standardHands, stage, rebound);
                    }
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(stage);
            }
        }

        private static void AlignModel(string model, IEnumerable<SkinSpec> skins, NeutralHands standard, Scene stage, HashSet<Mesh> rebound)
        {
            if (AssetImporter.GetAtPath(model) is not ModelImporter importer) return;

            NeutralHands hands = NeutralHands.Of(model, stage);
            if (hands == null)
            {
                Log.Warn($"Skin model {model} has no humanoid avatar, its hands stay as they are");
                return;
            }

            Quaternion right = Quaternion.Inverse(hands.Right) * standard.Right;
            Quaternion left = Quaternion.Inverse(hands.Left) * standard.Left;
            if (Angle(right) < Tolerance && Angle(left) < Tolerance) return;

            HandAxisPostprocessor.HandTurns turns = HandAxisPostprocessor.HandTurns.Read(importer.userData) ?? new HandAxisPostprocessor.HandTurns();
            turns.Add(hands.RightBone, right);
            turns.Add(hands.LeftBone, left);
            importer.userData = JsonUtility.ToJson(turns);
            importer.humanDescription = TurnedHumanDescription(importer.humanDescription, hands, right, left);
            importer.SaveAndReimport();

            foreach (SkinSpec skin in skins)
            {
                TurnUnpackedSkin(skin, model, right, left, rebound);
            }

            Log.Info($"Skin model {Path.GetFileNameWithoutExtension(model)} turned its hands to the standard axes: right by {Angle(right):F0}°, left by {Angle(left):F0}°");
        }

        private static void TurnUnpackedSkin(SkinSpec skin, string model, Quaternion right, Quaternion left, HashSet<Mesh> rebound)
        {
            string path = AssetDatabase.GetAssetPath(skin.Model);
            if (path == model) return;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Animator animator = contents.GetComponent<Animator>();
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

                bool ownBones = PrefabUtility.GetCorrespondingObjectFromOriginalSource(rightHand) == null;
                if (ownBones)
                {
                    HandAxisPostprocessor.Turn(rightHand, right);
                    HandAxisPostprocessor.Turn(leftHand, left);
                }

                foreach (SkinnedMeshRenderer mesh in contents.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (mesh.sharedMesh == null || AssetDatabase.GetAssetPath(mesh.sharedMesh) == model) continue;
                    if (!rebound.Add(mesh.sharedMesh)) continue;

                    bool turned = HandAxisPostprocessor.Rebind(mesh.sharedMesh, mesh.bones, rightHand, right) | HandAxisPostprocessor.Rebind(mesh.sharedMesh, mesh.bones, leftHand, left);
                    if (!turned) continue;

                    EditorUtility.SetDirty(mesh.sharedMesh);
                    AssetDatabase.SaveAssetIfDirty(mesh.sharedMesh);
                    Log.Debug($"Mesh {AssetDatabase.GetAssetPath(mesh.sharedMesh)} of skin {skin.name} rebound to the turned hands");
                }

                if (ownBones)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    Log.Debug($"Skin {skin.name} turned the hands of its own bones copy in {path}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static HumanDescription TurnedHumanDescription(HumanDescription description, NeutralHands hands, Quaternion right, Quaternion left)
        {
            SkeletonBone[] skeleton = description.skeleton;
            for (int i = 0; i < skeleton.Length; i++)
            {
                SkeletonBone bone = skeleton[i];
                if (bone.name == hands.RightBone)
                {
                    bone.rotation *= right;
                }
                else if (bone.name == hands.LeftBone)
                {
                    bone.rotation *= left;
                }
                else if (hands.RightChildren.Contains(bone.name))
                {
                    UndoTurn(ref bone, right);
                }
                else if (hands.LeftChildren.Contains(bone.name))
                {
                    UndoTurn(ref bone, left);
                }

                skeleton[i] = bone;
            }

            description.skeleton = skeleton;
            return description;
        }

        private static void UndoTurn(ref SkeletonBone bone, Quaternion turn)
        {
            Quaternion back = Quaternion.Inverse(turn);
            bone.position = back * bone.position;
            bone.rotation = back * bone.rotation;
        }

        private static IEnumerable<SkinSpec> Skins()
        {
            return AssetDatabase.FindAssets("t:" + nameof(SkinSpec))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkinSpec>)
                .Where(spec => spec != null && spec.Model != null);
        }

        private static string ModelPathOf(SkinSpec skin)
        {
            Animator animator = skin.Model.GetComponent<Animator>();
            return animator == null || animator.avatar == null ? null : AssetDatabase.GetAssetPath(animator.avatar);
        }

        private static float Angle(Quaternion turn)
        {
            return Quaternion.Angle(Quaternion.identity, turn);
        }

        private class NeutralHands
        {
            public Quaternion Right { get; private set; }
            public Quaternion Left { get; private set; }
            public string RightBone { get; private set; }
            public string LeftBone { get; private set; }
            public HashSet<string> RightChildren { get; private set; }
            public HashSet<string> LeftChildren { get; private set; }

            public static NeutralHands Of(string model, Scene stage)
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(model);
                if (asset == null) return null;

                GameObject body = Object.Instantiate(asset);
                SceneManager.MoveGameObjectToScene(body, stage);
                try
                {
                    body.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    Animator animator = body.GetComponent<Animator>();
                    if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return null;

                    Neutral(animator);
                    Transform right = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    Transform left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    return new NeutralHands
                    {
                        Right = right.rotation,
                        Left = left.rotation,
                        RightBone = right.name,
                        LeftBone = left.name,
                        RightChildren = ChildNames(right),
                        LeftChildren = ChildNames(left)
                    };
                }
                finally
                {
                    Object.DestroyImmediate(body);
                }
            }

            private static void Neutral(Animator animator)
            {
                using var handler = new HumanPoseHandler(animator.avatar, animator.transform);
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);
                pose.bodyRotation = Quaternion.identity;
                pose.muscles = new float[HumanTrait.MuscleCount];
                handler.SetHumanPose(ref pose);
            }

            private static HashSet<string> ChildNames(Transform hand)
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
