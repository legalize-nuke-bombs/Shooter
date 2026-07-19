using UnityEngine;

namespace Shooter.Client.Aiming
{
    public class Aim : MonoBehaviour
    {
        private const float Range = 1000f;

        public RaycastHit? Target { get; private set; }

        private Transform cameraTransform;

        private void Awake()
        {
            if (Application.isBatchMode) enabled = false;
        }

        private void Start()
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }

        private void Update()
        {
            Target = Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, Range)
                ? hit
                : (RaycastHit?)null;
        }
    }
}
