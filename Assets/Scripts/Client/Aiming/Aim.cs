using UnityEngine;

namespace Shooter.Client.Aiming
{
    public class Aim : MonoBehaviour
    {
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
            if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, 1000))
                Target = null;
            Target = hit;
        }
    }
}
