using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Animator))]
    public class Ragdoll : ShooterBehaviour
    {
        private const float LinearDamping = 0.5f;
        private const float AngularDamping = 1f;
        private static readonly Journal Log = Logs.Here();

        private void Start()
        {
            int layer = LayerMask.NameToLayer(Hitboxes.LayerName);
            if (layer < 0)
            {
                Log.Error($"Layer {Hitboxes.LayerName} is not defined, entity {name} gets no ragdoll");
                return;
            }

            Animator animator = GetComponent<Animator>();
            animator.enabled = false;

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (hips == null || head == null)
            {
                Log.Warn($"Entity {name} has no humanoid skeleton, ragdoll skipped");
                return;
            }

            float humanoidScale = Skeleton.MeasureHumanoidScale(hips, head);
            var rigidbodiesByBone = new Dictionary<HumanBodyBones, Rigidbody>();
            int builtCount = 0;

            foreach (Skeleton.Segment segment in Skeleton.Segments)
            {
                Transform bone = animator.GetBoneTransform(segment.From);
                Transform segmentEnd = Skeleton.FindSegmentEnd(animator, segment);
                if (bone == null || segmentEnd == null)
                {
                    Log.Warn($"Entity {name} misses bones {segment.From} - {segment.To}, ragdoll part skipped");
                    continue;
                }

                Rigidbody limbRigidbody = AddLimbCapsule(bone, segmentEnd.position, segment, segment.Radius * humanoidScale, layer);
                rigidbodiesByBone[segment.From] = limbRigidbody;

                if (!segment.IsRoot && rigidbodiesByBone.TryGetValue(segment.Parent, out Rigidbody parentRigidbody))
                {
                    ConnectWithJoint(bone, parentRigidbody);
                }

                builtCount++;
            }

            AddHeadSphere(head, hips, Skeleton.HeadRadius * humanoidScale, layer, rigidbodiesByBone);
            Log.Info($"Entity {name} turned into a ragdoll of {builtCount + 1} parts");
        }

        private Rigidbody AddLimbCapsule(Transform bone, Vector3 target, Skeleton.Segment segment, float radius, int layer)
        {
            Vector3 localTarget = bone.InverseTransformPoint(target);
            float thickness = radius / Skeleton.MeasureBoneScale(bone);

            CapsuleCollider capsule = bone.gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = GetLongestAxis(localTarget);
            capsule.radius = thickness;
            capsule.height = localTarget.magnitude + thickness * 1.5f;
            capsule.center = localTarget * 0.5f;

            return MakeRagdollPart(bone.gameObject, segment.Part, segment.Mass, layer, capsule);
        }

        private void AddHeadSphere(Transform head, Transform hips, float radius, int layer, Dictionary<HumanBodyBones, Rigidbody> rigidbodiesByBone)
        {
            Vector3 up = (head.position - hips.position).normalized;

            SphereCollider sphere = head.gameObject.AddComponent<SphereCollider>();
            sphere.radius = radius / Skeleton.MeasureBoneScale(head);
            sphere.center = head.InverseTransformPoint(head.position + up * (radius * 0.5f));

            MakeRagdollPart(head.gameObject, BodyPart.Head, Skeleton.HeadMass, layer, sphere);

            if (rigidbodiesByBone.TryGetValue(HumanBodyBones.Hips, out Rigidbody torsoRigidbody))
            {
                ConnectWithJoint(head, torsoRigidbody);
            }
        }

        private Rigidbody MakeRagdollPart(GameObject boneObject, BodyPart part, float mass, int layer, Collider shape)
        {
            boneObject.layer = layer;
            shape.excludeLayers = ~LayerMask.GetMask("Default", Hitboxes.LayerName);

            boneObject.AddComponent<Hitbox>().Part = part;

            Rigidbody partRigidbody = boneObject.AddComponent<Rigidbody>();
            partRigidbody.mass = mass;
            partRigidbody.linearDamping = LinearDamping;
            partRigidbody.angularDamping = AngularDamping;
            return partRigidbody;
        }

        private static void ConnectWithJoint(Transform bone, Rigidbody parentRigidbody)
        {
            CharacterJoint joint = bone.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = parentRigidbody;
            joint.enableProjection = true;
        }

        private static int GetLongestAxis(Vector3 vector)
        {
            float x = Mathf.Abs(vector.x);
            float y = Mathf.Abs(vector.y);
            float z = Mathf.Abs(vector.z);

            if (x >= y && x >= z) return 0;
            return y >= z ? 1 : 2;
        }
    }
}
