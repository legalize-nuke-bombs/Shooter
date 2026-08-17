using UnityEngine;

namespace Shooter.Game.Body
{
    public static class Skeleton
    {
        public const float SpineLength = 0.75f;
        public const float HeadRadius = 0.095f;
        public const float HeadRise = 0.13f;
        public const float HeadMass = 8f;

        public static readonly Segment[] Segments =
        {
            new(HumanBodyBones.Hips, HumanBodyBones.Neck, HumanBodyBones.Hips, BodyPart.Torso, 0.17f, 40f),
            new(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.Hips, BodyPart.Limbs, 0.06f,
                4f),
            new(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.LeftUpperArm, BodyPart.Limbs,
                0.05f, 3f),
            new(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.Hips, BodyPart.Limbs, 0.06f,
                4f),
            new(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.RightUpperArm, BodyPart.Limbs,
                0.05f, 3f),
            new(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.Hips, BodyPart.Limbs, 0.085f,
                10f),
            new(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftUpperLeg, BodyPart.Limbs,
                0.065f, 5f),
            new(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.Hips, BodyPart.Limbs, 0.085f,
                10f),
            new(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightUpperLeg, BodyPart.Limbs,
                0.065f, 5f)
        };

        public static Transform Ending(Animator animator, Segment segment)
        {
            Transform ending = animator.GetBoneTransform(segment.To);
            if (ending == null && segment.To == HumanBodyBones.Neck)
                ending = animator.GetBoneTransform(HumanBodyBones.Head);
            return ending;
        }

        public static float Scale(Transform hips, Transform head)
        {
            return Vector3.Distance(hips.position, head.position) / SpineLength;
        }

        public static float BoneScale(Transform bone)
        {
            Vector3 lossy = bone.lossyScale;
            return (lossy.x + lossy.y + lossy.z) / 3f;
        }

        public readonly struct Segment
        {
            public readonly HumanBodyBones From;
            public readonly HumanBodyBones To;
            public readonly HumanBodyBones Parent;
            public readonly BodyPart Part;
            public readonly float Radius;
            public readonly float Mass;

            public Segment(HumanBodyBones from, HumanBodyBones to, HumanBodyBones parent,
                BodyPart part, float radius, float mass)
            {
                From = from;
                To = to;
                Parent = parent;
                Part = part;
                Radius = radius;
                Mass = mass;
            }

            public bool Root => From == Parent;
        }
    }
}
