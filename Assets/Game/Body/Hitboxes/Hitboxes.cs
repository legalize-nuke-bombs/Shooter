using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Body.Hitboxes
{
    [RequireComponent(typeof(Animator))]
    public class Hitboxes : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public const string Layer = "Hitbox";

        private const float SpineLength = 0.75f;
        private const float HeadRadius = 0.13f;

        private static readonly Segment[] Segments =
        {
            new Segment(HumanBodyBones.Hips, HumanBodyBones.Neck, BodyPart.Torso, 0.17f),
            new Segment(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, BodyPart.Limbs, 0.06f),
            new Segment(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, BodyPart.Limbs, 0.05f),
            new Segment(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, BodyPart.Limbs, 0.06f),
            new Segment(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, BodyPart.Limbs, 0.05f),
            new Segment(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, BodyPart.Limbs, 0.085f),
            new Segment(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, BodyPart.Limbs, 0.065f),
            new Segment(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, BodyPart.Limbs, 0.085f),
            new Segment(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, BodyPart.Limbs, 0.065f)
        };

        private void Start()
        {
            int layer = LayerMask.NameToLayer(Layer);
            if (layer < 0)
            {
                Log.Error("Layer {} is not defined, entity {} gets no hitboxes", Layer, name);
                return;
            }

            var animator = GetComponent<Animator>();
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (hips == null || head == null)
            {
                Log.Warn("Entity {} has no humanoid skeleton, hitboxes skipped", name);
                return;
            }

            float scale = Vector3.Distance(hips.position, head.position) / SpineLength;
            int built = 0;

            foreach (Segment segment in Segments)
            {
                Transform from = animator.GetBoneTransform(segment.From);
                Transform to = Ending(animator, segment);
                if (from == null || to == null)
                {
                    Log.Warn("Entity {} misses bones {} - {}, hitbox skipped", name, segment.From, segment.To);
                    continue;
                }

                Pill(from, to.position, segment.Part, segment.Radius * scale, layer);
                built++;
            }

            Ball(head, (head.position - hips.position).normalized, HeadRadius * scale, layer);
            Log.Info("Entity {} got {} hitboxes, humanoid scale {}", name, built + 1, scale);
        }

        private static Transform Ending(Animator animator, Segment segment)
        {
            Transform ending = animator.GetBoneTransform(segment.To);
            if (ending == null && segment.To == HumanBodyBones.Neck)
                ending = animator.GetBoneTransform(HumanBodyBones.Head);
            return ending;
        }

        private void Pill(Transform bone, Vector3 target, BodyPart part, float radius, int layer)
        {
            GameObject mount = Mount(bone, part, layer);
            Vector3 reach = target - bone.position;
            mount.transform.rotation = Quaternion.FromToRotation(Vector3.up, reach.normalized);

            float grip = BoneScale(bone);
            float length = reach.magnitude / grip;
            float thickness = radius / grip;

            var pill = mount.AddComponent<CapsuleCollider>();
            pill.direction = 1;
            pill.radius = thickness;
            pill.height = length + thickness * 1.5f;
            pill.center = new Vector3(0f, length / 2f, 0f);
            Mute(pill);
        }

        private void Ball(Transform bone, Vector3 up, float radius, int layer)
        {
            GameObject mount = Mount(bone, BodyPart.Head, layer);

            var ball = mount.AddComponent<SphereCollider>();
            ball.radius = radius / BoneScale(bone);
            ball.center = mount.transform.InverseTransformPoint(bone.position + up * (radius * 0.5f));
            Mute(ball);
        }

        private GameObject Mount(Transform bone, BodyPart part, int layer)
        {
            var mount = new GameObject($"Hitbox {bone.name}");
            mount.layer = layer;
            mount.transform.SetParent(bone, false);

            var body = mount.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            mount.AddComponent<Hitbox>().Part = part;
            return mount;
        }

        private static void Mute(Collider hitbox)
        {
            hitbox.excludeLayers = ~0;
        }

        private static float BoneScale(Transform bone)
        {
            Vector3 lossy = bone.lossyScale;
            return (lossy.x + lossy.y + lossy.z) / 3f;
        }

        private readonly struct Segment
        {
            public readonly HumanBodyBones From;
            public readonly HumanBodyBones To;
            public readonly BodyPart Part;
            public readonly float Radius;

            public Segment(HumanBodyBones from, HumanBodyBones to, BodyPart part, float radius)
            {
                From = from;
                To = to;
                Part = part;
                Radius = radius;
            }
        }
    }
}
