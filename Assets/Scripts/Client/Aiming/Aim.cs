using UnityEngine;

namespace Shooter.Client.Aiming
{
    public class Aim
    {
        private const float Range = 1000f;

        public RaycastHit? Target { get; private set; }

        private readonly Transform cameraTransform;

        public Aim(Transform cameraTransform)
        {
            this.cameraTransform = cameraTransform;
        }

        public void Tick()
        {
            Target = Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, Range)
                ? hit
                : (RaycastHit?)null;
        }
    }
}
