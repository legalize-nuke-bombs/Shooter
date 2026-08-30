using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class Skin : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly Vector3 ModelOffset = new(0f, -1f, 0f);

        [SerializeField] private SkinSpec spec;

        public SkinSpec Spec => spec;

        public GameObject Flesh { get; private set; }

        private void Awake()
        {
            if (spec == null || spec.Model == null)
            {
                Log.Warn($"Entity {name} has no skin to wear, stays invisible");
                return;
            }

            Flesh = Instantiate(spec.Model, transform);
            Flesh.name = spec.Id.ToString();
            Flesh.transform.localPosition = new Vector3(0f, -1f, 0f);
            Flesh.transform.localRotation = Quaternion.identity;

            Animator animator = Flesh.GetComponent<Animator>();
            if (animator == null)
            {
                Log.Warn($"Skin {spec.Id} of entity {name} has no animator, entity stays still");
                return;
            }

            animator.runtimeAnimatorController = spec.Pose;
            animator.applyRootMotion = false;

            Flesh.AddComponent<Poser>();
            Flesh.AddComponent<Hitboxes>();

            Log.Info($"Entity {name} dressed as {spec.Id}, {Height(Flesh)} m tall");
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;

            CharacterMarker.Draw(transform.position + ModelOffset, gameObject.name);
        }

        private static float Height(GameObject flesh)
        {
            Renderer[] renderers = flesh.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);

            return bounds.size.y;
        }
    }
}
