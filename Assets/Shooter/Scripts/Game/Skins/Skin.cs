using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class Skin : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static readonly Vector3 ModelOffset = new(0f, -1f, 0f);

        [SerializeField] private SkinAsset asset;

        public SkinAsset Asset => asset;

        public GameObject ModelInstance { get; private set; }

        private void Awake()
        {
            if (asset == null || asset.Model == null)
            {
                Log.Warn($"Entity {name} has no skin to wear, stays invisible");
                return;
            }

            ModelInstance = Instantiate(asset.Model, transform);
            ModelInstance.name = asset.Id;
            ModelInstance.transform.localPosition = ModelOffset;
            ModelInstance.transform.localRotation = Quaternion.identity;

            Animator animator = ModelInstance.GetComponent<Animator>();
            if (animator == null)
            {
                Log.Warn($"Skin {asset.Id} of entity {name} has no animator, entity stays still");
                return;
            }

            animator.runtimeAnimatorController = asset.AnimatorController;
            animator.applyRootMotion = false;

            ShooterEntity shooterEntity = ModelInstance.AddComponent<ShooterEntity>();
            shooterEntity.Placement = WorldPlacement.Movable;
            shooterEntity.AddComponent<LocomotionAnimator>();
            shooterEntity.AddComponent<Hitboxes>();

            Log.Info($"Entity {name} dressed as {asset.Id}, {MeasureHeight(ModelInstance)} m tall");
        }

        public void SetModelVisible(bool visible)
        {
            if (ModelInstance == null) return;

            ModelInstance.SetActive(visible);
        }

        private static float MeasureHeight(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds.size.y;
        }
    }
}
