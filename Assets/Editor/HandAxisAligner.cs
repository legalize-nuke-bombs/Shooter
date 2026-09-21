using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shooter.Game.Body;
using Shooter.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Shooter.Editing
{
    public class HandAxisAligner : AssetPostprocessor
    {
        private const string Standard = "Assets/Game/Body/Appearance/Data/Adventurer/Adventurer.fbx";
        private const float Tolerance = 0.5f;
        private static readonly Journal Log = Logs.Here();

        public override uint GetVersion()
        {
            return 2;
        }

        // Bones turn before the avatar is built from them, meshes rebind once the whole model is there
        private void OnPostprocessMeshHierarchy(GameObject root)
        {
            HandTurns turns = HandTurns.Read(assetImporter.userData);
            if (turns == null) return;

            foreach (HandTurn turn in turns.hands)
            {
                Transform hand = Find(root, turn.bone);
                if (hand != null) Turn(hand, Quaternion.Euler(turn.euler));
            }
        }

        private void OnPostprocessModel(GameObject root)
        {
            HandTurns turns = HandTurns.Read(assetImporter.userData);
            if (turns == null) return;

            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (HandTurn turn in turns.hands)
            {
                Transform hand = Find(root, turn.bone);
                if (hand == null)
                {
                    Log.Warn($"Model {assetPath} has no bone {turn.bone} to turn to the standard hand axes");
                    continue;
                }

                var rebound = new HashSet<Mesh>();
                foreach (SkinnedMeshRenderer skin in skins)
                    if (rebound.Add(skin.sharedMesh))
                        Rebind(skin.sharedMesh, skin.bones, hand, Quaternion.Euler(turn.euler));
            }
        }

        private static Transform Find(GameObject root, string bone)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(found => found.name == bone);
        }

        [MenuItem("Tools/Align Hand Axes")]
        private static void Align()
        {
            Scene stage = EditorSceneManager.NewPreviewScene();
            try
            {
                Palms standard = Palms.Of(Standard, stage);
                if (standard == null)
                {
                    Log.Error($"Standard skin model {Standard} is missing or has no humanoid avatar");
                    return;
                }

                var rebound = new HashSet<Mesh>();
                foreach (IGrouping<string, SkinSpec> worn in Skins().GroupBy(Source))
                    if (worn.Key != null && worn.Key != Standard)
                        Align(worn.Key, worn, standard, stage, rebound);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(stage);
            }
        }

        private static void Align(string model, IEnumerable<SkinSpec> skins, Palms standard, Scene stage,
            HashSet<Mesh> rebound)
        {
            if (AssetImporter.GetAtPath(model) is not ModelImporter importer) return;

            Palms palms = Palms.Of(model, stage);
            if (palms == null)
            {
                Log.Warn($"Skin model {model} has no humanoid avatar, its hands stay as they are");
                return;
            }

            Quaternion right = Quaternion.Inverse(palms.Right) * standard.Right;
            Quaternion left = Quaternion.Inverse(palms.Left) * standard.Left;
            if (Angle(right) < Tolerance && Angle(left) < Tolerance) return;

            HandTurns turns = HandTurns.Read(importer.userData) ?? new HandTurns();
            turns.Add(palms.RightBone, right);
            turns.Add(palms.LeftBone, left);
            importer.userData = JsonUtility.ToJson(turns);
            importer.humanDescription = Posed(importer.humanDescription, palms, right, left);
            importer.SaveAndReimport();

            foreach (SkinSpec skin in skins) Follow(skin, model, right, left, rebound);

            Log.Info(
                $"Skin model {Path.GetFileNameWithoutExtension(model)} turned its hands to the standard axes: right by {Angle(right):F0}°, left by {Angle(left):F0}°");
        }

        // Unpacked prefabs own a copy of the bones and merged meshes their bindposes, so both turn with the model
        private static void Follow(SkinSpec skin, string model, Quaternion right, Quaternion left,
            HashSet<Mesh> rebound)
        {
            string path = AssetDatabase.GetAssetPath(skin.Model);
            if (path == model) return;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Animator animator = contents.GetComponent<Animator>();
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

                bool own = PrefabUtility.GetCorrespondingObjectFromOriginalSource(rightHand) == null;
                if (own)
                {
                    Turn(rightHand, right);
                    Turn(leftHand, left);
                }

                foreach (SkinnedMeshRenderer mesh in contents.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (mesh.sharedMesh == null || AssetDatabase.GetAssetPath(mesh.sharedMesh) == model) continue;
                    if (!rebound.Add(mesh.sharedMesh)) continue;

                    bool turned = Rebind(mesh.sharedMesh, mesh.bones, rightHand, right) |
                                  Rebind(mesh.sharedMesh, mesh.bones, leftHand, left);
                    if (!turned) continue;

                    EditorUtility.SetDirty(mesh.sharedMesh);
                    AssetDatabase.SaveAssetIfDirty(mesh.sharedMesh);
                    Log.Info(
                        $"Mesh {AssetDatabase.GetAssetPath(mesh.sharedMesh)} of skin {skin.name} rebound to the turned hands");
                }

                if (own)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    Log.Info($"Skin {skin.name} turned the hands of its own bones copy in {path}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void Turn(Transform hand, Quaternion turn)
        {
            Quaternion back = Quaternion.Inverse(turn);
            hand.localRotation *= turn;

            foreach (Transform child in hand)
            {
                child.localPosition = back * child.localPosition;
                child.localRotation = back * child.localRotation;
            }
        }

        private static bool Rebind(Mesh mesh, Transform[] bones, Transform hand, Quaternion turn)
        {
            int index = Array.IndexOf(bones, hand);
            if (mesh == null || index < 0 || index >= mesh.bindposes.Length) return false;

            Matrix4x4[] bindposes = mesh.bindposes;
            bindposes[index] = Matrix4x4.Rotate(Quaternion.Inverse(turn)) * bindposes[index];
            mesh.bindposes = bindposes;
            return true;
        }

        private static HumanDescription Posed(HumanDescription description, Palms palms, Quaternion right,
            Quaternion left)
        {
            SkeletonBone[] skeleton = description.skeleton;

            for (int i = 0; i < skeleton.Length; i++)
            {
                SkeletonBone bone = skeleton[i];
                if (bone.name == palms.RightBone) bone.rotation *= right;
                else if (bone.name == palms.LeftBone) bone.rotation *= left;
                else if (palms.RightChildren.Contains(bone.name)) Back(ref bone, right);
                else if (palms.LeftChildren.Contains(bone.name)) Back(ref bone, left);
                skeleton[i] = bone;
            }

            description.skeleton = skeleton;
            return description;
        }

        private static void Back(ref SkeletonBone bone, Quaternion turn)
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

        private static string Source(SkinSpec skin)
        {
            Animator animator = skin.Model.GetComponent<Animator>();
            return animator == null || animator.avatar == null ? null : AssetDatabase.GetAssetPath(animator.avatar);
        }

        private static float Angle(Quaternion turn)
        {
            return Quaternion.Angle(Quaternion.identity, turn);
        }

        private class Palms
        {
            public Quaternion Right { get; private set; }
            public Quaternion Left { get; private set; }
            public string RightBone { get; private set; }
            public string LeftBone { get; private set; }
            public HashSet<string> RightChildren { get; private set; }
            public HashSet<string> LeftChildren { get; private set; }

            public static Palms Of(string model, Scene stage)
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

                    return new Palms
                    {
                        Right = right.rotation,
                        Left = left.rotation,
                        RightBone = right.name,
                        LeftBone = left.name,
                        RightChildren = Children(right),
                        LeftChildren = Children(left)
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

            private static HashSet<string> Children(Transform hand)
            {
                var names = new HashSet<string>();
                foreach (Transform child in hand) names.Add(child.name);
                return names;
            }
        }

        [Serializable]
        private class HandTurns
        {
            public List<HandTurn> hands = new();

            public static HandTurns Read(string userData)
            {
                if (string.IsNullOrEmpty(userData)) return null;

                try
                {
                    HandTurns turns = JsonUtility.FromJson<HandTurns>(userData);
                    return turns?.hands != null && turns.hands.Count > 0 ? turns : null;
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            public void Add(string bone, Quaternion turn)
            {
                HandTurn known = hands.FirstOrDefault(hand => hand.bone == bone);
                if (known == null) hands.Add(new HandTurn { bone = bone, euler = turn.eulerAngles });
                else known.euler = (Quaternion.Euler(known.euler) * turn).eulerAngles;
            }
        }

        [Serializable]
        private class HandTurn
        {
            public string bone;
            public Vector3 euler;
        }
    }
}
