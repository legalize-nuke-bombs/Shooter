using UnityEngine;
using Shooter.Client.Entities.Npcs;

namespace Shooter.Client.Aiming
{
    public class Aim : MonoBehaviour
    {
        private const float Reach = 12f;

        public NpcAvatar Target { get; private set; }

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
            Target = Probe();
        }

        private NpcAvatar Probe()
        {
            if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, Reach))
                return null;
            return hit.transform.TryGetComponent(out NpcBody body) ? body.Avatar : null;
        }
    }
}
