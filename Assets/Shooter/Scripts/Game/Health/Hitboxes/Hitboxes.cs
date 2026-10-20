using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Animator))]
    public class Hitboxes : MonoBehaviour
    {
        public const string LayerName = "Hitbox";

        private const float MinHumanoidScale = 0.6f;
        private const float MaxHumanoidScale = 1.4f;
        private static readonly Journal Log = Logs.Here();

        private void Start()
        {
            int layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0)
            {
                Log.Error($"Layer {LayerName} is not defined, entity {name} gets no hitboxes");
                return;
            }

            Animator animator = GetComponent<Animator>();
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (hips == null || head == null)
            {
                Log.Warn($"Entity {name} has no humanoid skeleton, hitboxes skipped");
                return;
            }

            float humanoidScale = Skeleton.MeasureHumanoidScale(hips, head);
            if (humanoidScale < MinHumanoidScale || humanoidScale > MaxHumanoidScale)
            {
                Log.Error($"Entity {name} measures {Vector3.Distance(hips.position, head.position)} from hips to head, a humanoid scale of {humanoidScale}: its avatar likely maps Hips to a bone that is not the pelvis");
            }

            int builtCount = 0;

            foreach (Skeleton.Segment segment in Skeleton.Segments)
            {
                Transform segmentStart = animator.GetBoneTransform(segment.From);
                Transform segmentEnd = Skeleton.FindSegmentEnd(animator, segment);
                if (segmentStart == null || segmentEnd == null)
                {
                    Log.Warn($"Entity {name} misses bones {segment.From} - {segment.To}, hitbox skipped");
                    continue;
                }

                AddHitboxCapsule(segmentStart, segmentEnd.position, segment.Part, segment.Radius * humanoidScale, layer);
                builtCount++;
            }

            Vector3 crown = head.position + (head.position - hips.position).normalized * (Skeleton.HeadRise * humanoidScale);
            AddHitboxCapsule(head, crown, BodyPart.Head, Skeleton.HeadRadius * humanoidScale, layer);
            Log.Info($"Entity {name} got {builtCount + 1} hitboxes, humanoid scale {humanoidScale}");
        }

        private void AddHitboxCapsule(Transform bone, Vector3 target, BodyPart part, float radius, int layer)
        {
            GameObject hitboxObject = CreateHitboxObject(bone, part, layer);
            Vector3 boneToTarget = target - bone.position;
            hitboxObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, boneToTarget.normalized);

            float boneScale = Skeleton.MeasureBoneScale(bone);
            float length = boneToTarget.magnitude / boneScale;
            float thickness = radius * part.GetHitboxGenerosity() / boneScale;

            CapsuleCollider capsule = hitboxObject.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = thickness;
            capsule.height = length + thickness * 1.5f;
            capsule.center = new Vector3(0f, length / 2f, 0f);
            ExcludeFromCollisions(capsule);
        }

        private GameObject CreateHitboxObject(Transform bone, BodyPart part, int layer)
        {
            GameObject hitboxObject = new($"Hitbox {bone.name}");
            hitboxObject.layer = layer;
            hitboxObject.transform.SetParent(bone, false);

            Rigidbody hitboxRigidbody = hitboxObject.AddComponent<Rigidbody>();
            hitboxRigidbody.isKinematic = true;
            hitboxRigidbody.useGravity = false;

            hitboxObject.AddComponent<Hitbox>().Part = part;
            return hitboxObject;
        }

        private static void ExcludeFromCollisions(Collider hitbox)
        {
            hitbox.excludeLayers = ~0;
        }
    }
}
