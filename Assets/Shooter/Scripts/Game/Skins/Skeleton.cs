using UnityEngine;

namespace Shooter.Game
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
            new(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.Hips, BodyPart.Limbs, 0.06f, 4f),
            new(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.LeftUpperArm, BodyPart.Limbs, 0.05f, 3f),
            new(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.Hips, BodyPart.Limbs, 0.06f, 4f),
            new(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.RightUpperArm, BodyPart.Limbs, 0.05f, 3f),
            new(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.Hips, BodyPart.Limbs, 0.085f, 10f),
            new(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftUpperLeg, BodyPart.Limbs, 0.065f, 5f),
            new(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.Hips, BodyPart.Limbs, 0.085f, 10f),
            new(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightUpperLeg, BodyPart.Limbs, 0.065f, 5f)
        };

        public static Transform FindSegmentEnd(Animator animator, Segment segment)
        {
            Transform segmentEnd = animator.GetBoneTransform(segment.To);
            if (segmentEnd == null && segment.To == HumanBodyBones.Neck)
            {
                segmentEnd = animator.GetBoneTransform(HumanBodyBones.Head);
            }

            return segmentEnd;
        }

        public static float MeasureHumanoidScale(Transform hips, Transform head)
        {
            return Vector3.Distance(hips.position, head.position) / SpineLength;
        }

        public static float MeasureBoneScale(Transform bone)
        {
            Vector3 lossyScale = bone.lossyScale;
            return (lossyScale.x + lossyScale.y + lossyScale.z) / 3f;
        }

        public readonly struct Segment
        {
            public readonly HumanBodyBones From;
            public readonly HumanBodyBones To;
            public readonly HumanBodyBones Parent;
            public readonly BodyPart Part;
            public readonly float Radius;
            public readonly float Mass;

            public Segment(HumanBodyBones from, HumanBodyBones to, HumanBodyBones parent, BodyPart part, float radius, float mass)
            {
                From = from;
                To = to;
                Parent = parent;
                Part = part;
                Radius = radius;
                Mass = mass;
            }

            public bool IsRoot => From == Parent;
        }
    }
}
