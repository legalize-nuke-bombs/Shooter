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
                Log.Error("Entity {} has no skin to wear, stays invisible", name);
                return;
            }

            Flesh = Instantiate(spec.Model, transform);
            Flesh.name = spec.Id.ToString();
            Flesh.transform.localPosition = new Vector3(0f, -1f, 0f);
            Flesh.transform.localRotation = Quaternion.identity;

            var animator = Flesh.GetComponent<Animator>();
            if (animator == null)
            {
                Log.Error("Skin {} of entity {} has no animator, entity stays still", spec.Id, name);
                return;
            }

            animator.runtimeAnimatorController = spec.Pose;
            animator.applyRootMotion = false;

            Flesh.AddComponent<Poser>();
            Flesh.AddComponent<Hitboxes.Hitboxes>();

            Log.Info("Entity {} dressed as {}", name, spec.Id);
        }
    }
}
