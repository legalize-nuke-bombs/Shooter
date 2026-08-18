using System.Collections.Generic;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Animator))]
    public class Ragdoll : MonoBehaviour
    {
        private const float Damping = 0.5f;
        private const float AngularDamping = 1f;
        private static readonly Journal Log = Logs.Here();

        private void Start()
        {
            int layer = LayerMask.NameToLayer(Hitboxes.Layer);
            if (layer < 0)
            {
                Log.Error($"Layer {Hitboxes.Layer} is not defined, entity {name} gets no ragdoll");
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

            float scale = Skeleton.Scale(hips, head);
            var bodies = new Dictionary<HumanBodyBones, Rigidbody>();
            int built = 0;

            foreach (Skeleton.Segment segment in Skeleton.Segments)
            {
                Transform bone = animator.GetBoneTransform(segment.From);
                Transform ending = Skeleton.Ending(animator, segment);
                if (bone == null || ending == null)
                {
                    Log.Warn(
                        $"Entity {name} misses bones {segment.From} - {segment.To}, ragdoll part skipped");
                    continue;
                }

                Rigidbody body = Limb(bone, ending.position, segment, segment.Radius * scale, layer);
                bodies[segment.From] = body;

                if (!segment.Root && bodies.TryGetValue(segment.Parent, out Rigidbody parent))
                    Connect(bone, parent);

                built++;
            }

            Skull(head, hips, Skeleton.HeadRadius * scale, layer, bodies);
            Log.Info($"Entity {name} turned into a ragdoll of {built + 1} parts");
        }

        private Rigidbody Limb(Transform bone, Vector3 target, Skeleton.Segment segment, float radius, int layer)
        {
            Vector3 local = bone.InverseTransformPoint(target);
            float thickness = radius / Skeleton.BoneScale(bone);

            CapsuleCollider pill = bone.gameObject.AddComponent<CapsuleCollider>();
            pill.direction = Longest(local);
            pill.radius = thickness;
            pill.height = local.magnitude + thickness * 1.5f;
            pill.center = local * 0.5f;

            return Fill(bone.gameObject, segment.Part, segment.Mass, layer, pill);
        }

        private void Skull(Transform head, Transform hips, float radius, int layer,
            Dictionary<HumanBodyBones, Rigidbody> bodies)
        {
            Vector3 up = (head.position - hips.position).normalized;

            SphereCollider ball = head.gameObject.AddComponent<SphereCollider>();
            ball.radius = radius / Skeleton.BoneScale(head);
            ball.center = head.InverseTransformPoint(head.position + up * (radius * 0.5f));

            Fill(head.gameObject, BodyPart.Head, Skeleton.HeadMass, layer, ball);

            if (bodies.TryGetValue(HumanBodyBones.Hips, out Rigidbody torso))
                Connect(head, torso);
        }

        private Rigidbody Fill(GameObject bone, BodyPart part, float mass, int layer, Collider shape)
        {
            bone.layer = layer;
            shape.excludeLayers = ~LayerMask.GetMask("Default", Hitboxes.Layer);

            bone.AddComponent<Hitbox>().Part = part;

            Rigidbody body = bone.AddComponent<Rigidbody>();
            body.mass = mass;
            body.linearDamping = Damping;
            body.angularDamping = AngularDamping;
            return body;
        }

        private static void Connect(Transform bone, Rigidbody parent)
        {
            CharacterJoint joint = bone.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = parent;
            joint.enableProjection = true;
        }

        private static int Longest(Vector3 local)
        {
            float x = Mathf.Abs(local.x);
            float y = Mathf.Abs(local.y);
            float z = Mathf.Abs(local.z);

            if (x >= y && x >= z) return 0;
            return y >= z ? 1 : 2;
        }
    }
}
