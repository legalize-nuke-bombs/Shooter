using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public class HandAxisPostprocessor : AssetPostprocessor
    {
        private static readonly Journal Log = Logs.Here();

        public override uint GetVersion()
        {
            return 2;
        }

        private void OnPostprocessMeshHierarchy(GameObject root)
        {
            HandTurns handTurns = HandTurns.ReadFromUserData(assetImporter.userData);
            if (handTurns == null) return;

            foreach (HandTurn handTurn in handTurns.hands)
            {
                Transform hand = FindBone(root, handTurn.bone);
                if (hand != null)
                {
                    TurnHandKeepingChildrenInPlace(hand, Quaternion.Euler(handTurn.euler));
                }
            }
        }

        private void OnPostprocessModel(GameObject root)
        {
            HandTurns handTurns = HandTurns.ReadFromUserData(assetImporter.userData);
            if (handTurns == null) return;

            SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (HandTurn handTurn in handTurns.hands)
            {
                Transform hand = FindBone(root, handTurn.bone);
                if (hand == null)
                {
                    Log.Warn($"Model {assetPath} has no bone {handTurn.bone} to turn to the standard hand axes");
                    continue;
                }

                var reboundMeshes = new HashSet<Mesh>();
                foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
                {
                    if (reboundMeshes.Add(skinnedRenderer.sharedMesh))
                    {
                        TryRebindMeshToTurnedHand(skinnedRenderer.sharedMesh, skinnedRenderer.bones, hand, Quaternion.Euler(handTurn.euler));
                    }
                }
            }
        }

        internal static void TurnHandKeepingChildrenInPlace(Transform hand, Quaternion turn)
        {
            Quaternion inverseTurn = Quaternion.Inverse(turn);
            hand.localRotation *= turn;
            foreach (Transform child in hand)
            {
                child.localPosition = inverseTurn * child.localPosition;
                child.localRotation = inverseTurn * child.localRotation;
            }
        }

        internal static bool TryRebindMeshToTurnedHand(Mesh mesh, Transform[] bones, Transform hand, Quaternion turn)
        {
            int handIndex = Array.IndexOf(bones, hand);
            if (mesh == null || handIndex < 0 || handIndex >= mesh.bindposes.Length) return false;

            Matrix4x4[] bindposes = mesh.bindposes;
            bindposes[handIndex] = Matrix4x4.Rotate(Quaternion.Inverse(turn)) * bindposes[handIndex];
            mesh.bindposes = bindposes;
            return true;
        }

        private static Transform FindBone(GameObject root, string boneName)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(bone => bone.name == boneName);
        }

        [Serializable]
        internal class HandTurns
        {
            public List<HandTurn> hands = new();

            public static HandTurns ReadFromUserData(string userData)
            {
                if (string.IsNullOrEmpty(userData)) return null;

                try
                {
                    HandTurns handTurns = JsonUtility.FromJson<HandTurns>(userData);
                    return handTurns?.hands != null && handTurns.hands.Count > 0 ? handTurns : null;
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            public void AddTurn(string boneName, Quaternion turn)
            {
                HandTurn knownTurn = hands.FirstOrDefault(handTurn => handTurn.bone == boneName);
                if (knownTurn == null)
                {
                    hands.Add(new HandTurn { bone = boneName, euler = turn.eulerAngles });
                }
                else
                {
                    knownTurn.euler = (Quaternion.Euler(knownTurn.euler) * turn).eulerAngles;
                }
            }
        }

        [Serializable]
        internal class HandTurn
        {
            public string bone;
            public Vector3 euler;
        }
    }
}
