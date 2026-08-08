using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Body.Hitboxes
{
    [RequireComponent(typeof(Animator))]
    public class Hitboxes : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public const string Layer = "Hitbox";

        private const float LeastScale = 0.6f;
        private const float MostScale = 1.4f;

        private void Start()
        {
            int layer = LayerMask.NameToLayer(Layer);
            if (layer < 0)
            {
                Log.Error($"Layer {Layer} is not defined, entity {name} gets no hitboxes");
                return;
            }

            var animator = GetComponent<Animator>();
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (hips == null || head == null)
            {
                Log.Warn($"Entity {name} has no humanoid skeleton, hitboxes skipped");
                return;
            }

            float scale = Skeleton.Scale(hips, head);
            if (scale < LeastScale || scale > MostScale)
                Log.Error($"Entity {name} measures {(Vector3.Distance(hips.position, head.position))} from hips to head, a humanoid scale of {scale}: its avatar likely maps Hips to a bone that is not the pelvis");

            int built = 0;

            foreach (Skeleton.Segment segment in Skeleton.Segments)
            {
                Transform from = animator.GetBoneTransform(segment.From);
                Transform to = Skeleton.Ending(animator, segment);
                if (from == null || to == null)
                {
                    Log.Warn($"Entity {name} misses bones {segment.From} - {segment.To}, hitbox skipped");
                    continue;
                }

                Pill(from, to.position, segment.Part, segment.Radius * scale, layer);
                built++;
            }

            Vector3 crown = head.position + (head.position - hips.position).normalized * (Skeleton.HeadRise * scale);
            Pill(head, crown, BodyPart.Head, Skeleton.HeadRadius * scale, layer);
            Log.Info($"Entity {name} got {(built + 1)} hitboxes, humanoid scale {scale}");
        }

        private void Pill(Transform bone, Vector3 target, BodyPart part, float radius, int layer)
        {
            GameObject mount = Mount(bone, part, layer);
            Vector3 reach = target - bone.position;
            mount.transform.rotation = Quaternion.FromToRotation(Vector3.up, reach.normalized);

            float grip = Skeleton.BoneScale(bone);
            float length = reach.magnitude / grip;
            float thickness = radius * part.Generosity() / grip;

            var pill = mount.AddComponent<CapsuleCollider>();
            pill.direction = 1;
            pill.radius = thickness;
            pill.height = length + thickness * 1.5f;
            pill.center = new Vector3(0f, length / 2f, 0f);
            Mute(pill);
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
    }
}
