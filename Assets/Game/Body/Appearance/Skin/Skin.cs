using System.Collections.Generic;
using Shooter.Game.Body.Appearance;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class Skin : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private SkinSpec spec;

        public SkinSpec Spec => spec;

        public GameObject Flesh { get; private set; }

        private void Awake()
        {
            if (spec == null || spec.Model == null)
            {
                Log.Error($"Entity {name} has no skin to wear, stays invisible");
                return;
            }

            Flesh = Instantiate(spec.Model, transform);
            Flesh.name = spec.Id.ToString();
            Flesh.transform.localPosition = new Vector3(0f, -1f, 0f);
            Flesh.transform.localRotation = Quaternion.identity;

            var animator = Flesh.GetComponent<Animator>();
            if (animator == null)
            {
                Log.Error($"Skin {spec.Id} of entity {name} has no animator, entity stays still");
                return;
            }

            animator.runtimeAnimatorController = spec.Pose;
            animator.applyRootMotion = false;

            Flesh.AddComponent<Poser>();
            Flesh.AddComponent<Hitboxes.Hitboxes>();

            Log.Info($"Entity {name} dressed as {spec.Id}, {(Height(Flesh))} m tall");
        }

        private static float Height(GameObject flesh)
        {
            var renderers = flesh.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);

            return bounds.size.y;
        }

        private static readonly Vector3 ModelOffset = new Vector3(0f, -1f, 0f);
        private static readonly Color GizmoTint = new Color(0.35f, 0.9f, 1f, 0.4f);
        private const float GizmoRing = 0.4f;
        private const float GizmoHeight = 2f;

        private struct GizmoBody
        {
            public GameObject Model;
            public Mesh[] Meshes;
            public Matrix4x4[] Places;
        }

        private static readonly Dictionary<SkinSpec, GizmoBody> GizmoCache = new Dictionary<SkinSpec, GizmoBody>();

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;

            Gizmos.color = GizmoTint;

            if (spec == null || spec.Model == null)
            {
                Vector3 feet = transform.position + ModelOffset;
                Gizmos.DrawWireSphere(feet, GizmoRing);
                Gizmos.DrawLine(feet, feet + Vector3.up * GizmoHeight);
                return;
            }

            Matrix4x4 place = transform.localToWorldMatrix * Matrix4x4.Translate(ModelOffset);
            GizmoBody body = Body(spec);

            for (int part = 0; part < body.Meshes.Length; part++)
            {
                Gizmos.matrix = place * body.Places[part];
                Gizmos.DrawMesh(body.Meshes[part]);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        private static GizmoBody Body(SkinSpec spec)
        {
            if (GizmoCache.TryGetValue(spec, out GizmoBody cached) && cached.Model == spec.Model) return cached;

            Transform root = spec.Model.transform;
            Matrix4x4 rootScale = Matrix4x4.Scale(root.localScale);
            var meshes = new List<Mesh>();
            var places = new List<Matrix4x4>();

            foreach (SkinnedMeshRenderer skinned in spec.Model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skinned.sharedMesh == null) continue;

                Transform[] bones = skinned.bones;
                Matrix4x4[] binds = skinned.sharedMesh.bindposes;
                Matrix4x4 pose = bones.Length > 0 && binds.Length > 0 && bones[0] != null
                    ? bones[0].localToWorldMatrix * binds[0]
                    : skinned.transform.localToWorldMatrix;

                meshes.Add(skinned.sharedMesh);
                places.Add(rootScale * root.worldToLocalMatrix * pose);
            }

            foreach (MeshFilter filter in spec.Model.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;

                meshes.Add(filter.sharedMesh);
                places.Add(rootScale * root.worldToLocalMatrix * filter.transform.localToWorldMatrix);
            }

            var body = new GizmoBody { Model = spec.Model, Meshes = meshes.ToArray(), Places = places.ToArray() };
            GizmoCache[spec] = body;
            return body;
        }
    }
}
