using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public sealed class LookHitFinder : MonoBehaviour
    {
        [SerializeField] private float reach = 10f;

        private int lastCastFrame = -1;
        private RaycastHit lookHit;
        private bool hasLookHit;

        public float Reach => reach;

        public bool TryFindLookHit(out RaycastHit hit)
        {
            CastOncePerFrame();

            hit = lookHit;
            return hasLookHit;
        }

        private void CastOncePerFrame()
        {
            if (lastCastFrame == Time.frameCount) return;

            lastCastFrame = Time.frameCount;
            hasLookHit = false;

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Transform cameraTransform = mainCamera.transform;
            hasLookHit = Interactor.TryFindNearestLookHit(cameraTransform.position, cameraTransform.forward, reach, cameraTransform.root, out lookHit);
        }
    }
}
