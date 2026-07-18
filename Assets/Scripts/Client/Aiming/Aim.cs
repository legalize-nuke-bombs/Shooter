using UnityEngine;
using Shooter.Client.Entities.Npcs;
using Shooter.Server.Worlds.Entities.Sleeping;

namespace Shooter.Client.Aiming
{
    public class Aim : MonoBehaviour
    {
        private const float Reach = 12f;

        public NpcAvatar Target { get; private set; }
        public float BedDistance { get; private set; } = float.PositiveInfinity;

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
            Target = null;
            BedDistance = float.PositiveInfinity;

            if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, Reach))
                return;

            if (hit.transform.TryGetComponent(out NpcBody body))
                Target = body.Avatar;
            else if (Sleep.IsBed(hit.transform.name))
                BedDistance = hit.distance;
        }
    }
}
