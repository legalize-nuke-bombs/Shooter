using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Movement))]
    public class Interactor : NetworkBehaviour
    {
        public const float EyeHeight = 0.75f;
        private static readonly Journal Log = Logs.Here();

        private static readonly RaycastHit[] LookHits = new RaycastHit[16];

        private static int lookMask;

        [SerializeField] private float reach = 3f;

        private Movement movement;

        private static int LookMask => lookMask != 0 ? lookMask : lookMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Character");

        public Vector3 EyePosition => transform.position + Vector3.up * EyeHeight;

        public Vector3 LookDirection => movement.LookDirection;

        public float ReachDistance => reach;

        private void Awake()
        {
            movement = GetComponent<Movement>();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void UseRpc()
        {
            if (!TryFindLookedAt(reach, out IUsable usable))
            {
                int seenCount = CollectLookHits(reach, LookHits);
                for (int i = 0; i < seenCount; i++)
                {
                    Log.Info($"Player {OwnerClientId} sees {LookHits[i].collider.name} on layer {LayerMask.LayerToName(LookHits[i].collider.gameObject.layer)} in {LookHits[i].distance}m, usable {(LookHits[i].collider.GetComponentInParent<IUsable>() == null ? "no" : "yes")}");
                }

                Log.Info($"Player {OwnerClientId} used nothing within {reach}m, saw {seenCount} colliders");
                return;
            }

            Log.Info($"Player {OwnerClientId} uses {((Component)usable).name}");
            usable.Use(NetworkObject, out _);
        }

        public bool TryFindNearestLookHit(float distance, out RaycastHit hit)
        {
            return TryFindNearestLookHit(EyePosition, movement.LookDirection, distance, transform, out hit);
        }

        public int CollectLookHits(float distance, RaycastHit[] hits)
        {
            return CollectLookHits(EyePosition, movement.LookDirection, distance, transform, hits);
        }

        public static bool TryFindNearestLookHit(Vector3 origin, Vector3 direction, float distance, Transform looker, out RaycastHit hit)
        {
            int hitCount = CollectLookHits(origin, direction, distance, looker, LookHits);
            return TryFindNearestHit(LookHits, hitCount, out hit);
        }

        public static int CollectLookHits(Vector3 origin, Vector3 direction, float distance, Transform looker, RaycastHit[] hits)
        {
            int hitCount = Physics.RaycastNonAlloc(origin, direction, hits, distance, LookMask, QueryTriggerInteraction.Ignore);
            int keptCount = 0;

            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].transform.IsChildOf(looker)) continue;
                hits[keptCount++] = hits[i];
            }

            return keptCount;
        }

        public static bool TryFindNearestHit(RaycastHit[] hits, int hitCount, out RaycastHit hit)
        {
            hit = default;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].distance >= closestDistance) continue;

                hit = hits[i];
                closestDistance = hits[i].distance;
            }

            return closestDistance < float.PositiveInfinity;
        }

        public bool TryFindLookedAt<T>(float distance, out T found) where T : class
        {
            found = null;
            if (!TryFindNearestLookHit(distance, out RaycastHit hit)) return false;
            if (hit.collider == null) return false;

            found = hit.collider.GetComponentInParent<T>();
            return found != null;
        }
    }
}
