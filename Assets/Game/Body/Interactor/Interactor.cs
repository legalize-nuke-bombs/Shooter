using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Movement))]
    public class Interactor : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public const float EyeHeight = 0.75f;

        private static readonly RaycastHit[] Sights = new RaycastHit[16];

        private static int lookMask;

        private static int LookMask => lookMask != 0 ? lookMask : lookMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Character");

        [SerializeField] private float reach = 3f;

        private Movement movement;

        public Vector3 Eyes => transform.position + Vector3.up * EyeHeight;

        public float Reach => reach;

        private void Awake()
        {
            movement = GetComponent<Movement>();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void UseRpc()
        {
            if (!TryReach(reach, out IUsable usable))
            {
                Log.Info("Player {} used nothing within {}m", OwnerClientId, reach);
                return;
            }

            Log.Info("Player {} uses {}", OwnerClientId, ((Component)usable).name);
            usable.Use(NetworkObject);
        }

        public bool TryLook(float distance, out RaycastHit hit)
        {
            return TryLook(Eyes, movement.Look, distance, transform, out hit);
        }

        public static bool TryLook(Vector3 origin, Vector3 direction, float distance, Transform looker, out RaycastHit hit)
        {
            int found = Physics.RaycastNonAlloc(origin, direction, Sights, distance, LookMask);
            hit = default;
            float closest = float.PositiveInfinity;

            for (int i = 0; i < found; i++)
            {
                RaycastHit candidate = Sights[i];
                if (candidate.distance >= closest) continue;
                if (candidate.transform.IsChildOf(looker)) continue;

                hit = candidate;
                closest = candidate.distance;
            }

            return closest < float.PositiveInfinity;
        }

        public bool TryReach<T>(float distance, out T found) where T : class
        {
            found = null;
            if (!TryLook(distance, out RaycastHit hit)) return false;
            if (hit.collider == null) return false;

            found = hit.collider.GetComponentInParent<T>();
            return found != null;
        }
    }
}
