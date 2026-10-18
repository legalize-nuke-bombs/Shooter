using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public class HandAxisAligner : AssetPostprocessor
    {
        private static readonly Journal Log = Logs.Here();

        public override uint GetVersion()
        {
            return 2;
        }

        private void OnPostprocessMeshHierarchy(GameObject root)
        {
            HandTurns turns = HandTurns.Read(assetImporter.userData);
            if (turns == null) return;

            foreach (HandTurn turn in turns.hands)
            {
                Transform hand = Find(root, turn.bone);
                if (hand != null)
                {
                    Turn(hand, Quaternion.Euler(turn.euler));
                }
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
                {
                    if (rebound.Add(skin.sharedMesh))
                    {
                        Rebind(skin.sharedMesh, skin.bones, hand, Quaternion.Euler(turn.euler));
                    }
                }
            }
        }

        internal static void Turn(Transform hand, Quaternion turn)
        {
            Quaternion back = Quaternion.Inverse(turn);
            hand.localRotation *= turn;
            foreach (Transform child in hand)
            {
                child.localPosition = back * child.localPosition;
                child.localRotation = back * child.localRotation;
            }
        }

        internal static bool Rebind(Mesh mesh, Transform[] bones, Transform hand, Quaternion turn)
        {
            int index = Array.IndexOf(bones, hand);
            if (mesh == null || index < 0 || index >= mesh.bindposes.Length) return false;

            Matrix4x4[] bindposes = mesh.bindposes;
            bindposes[index] = Matrix4x4.Rotate(Quaternion.Inverse(turn)) * bindposes[index];
            mesh.bindposes = bindposes;
            return true;
        }

        private static Transform Find(GameObject root, string bone)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(found => found.name == bone);
        }

        [Serializable]
        internal class HandTurns
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
                if (known == null)
                {
                    hands.Add(new HandTurn { bone = bone, euler = turn.eulerAngles });
                }
                else
                {
                    known.euler = (Quaternion.Euler(known.euler) * turn).eulerAngles;
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
