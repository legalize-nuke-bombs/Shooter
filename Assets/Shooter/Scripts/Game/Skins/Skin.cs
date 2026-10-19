using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class Skin : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly Vector3 ModelOffset = new(0f, -1f, 0f);

        [SerializeField] private SkinAsset asset;

        public SkinAsset Asset => asset;

        public GameObject Flesh { get; private set; }

        private void Awake()
        {
            if (asset == null || asset.Model == null)
            {
                Log.Warn($"Entity {name} has no skin to wear, stays invisible");
                return;
            }

            Flesh = Instantiate(asset.Model, transform);
            Flesh.name = asset.Id;
            Flesh.transform.localPosition = new Vector3(0f, -1f, 0f);
            Flesh.transform.localRotation = Quaternion.identity;

            Animator animator = Flesh.GetComponent<Animator>();
            if (animator == null)
            {
                Log.Warn($"Skin {asset.Id} of entity {name} has no animator, entity stays still");
                return;
            }

            animator.runtimeAnimatorController = asset.Pose;
            animator.applyRootMotion = false;

            Flesh.AddComponent<Poser>();
            Flesh.AddComponent<Hitboxes>();

            Log.Info($"Entity {name} dressed as {asset.Id}, {Height(Flesh)} m tall");
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
