using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Movement))]
    public class Interactor : NetworkBehaviour
    {
        public const float EyeHeight = 0.75f;
        private static readonly Journal Log = Logs.Here();

        private static readonly RaycastHit[] Sights = new RaycastHit[16];

        private static int lookMask;

        [SerializeField] private float reach = 3f;

        private Movement movement;

        private static int LookMask => lookMask != 0
            ? lookMask
            : lookMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Character");

        public Vector3 Eyes => transform.position + Vector3.up * EyeHeight;

        public Vector3 Sight => movement.Look;

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
                int seen = Look(reach, Sights);
                for (int i = 0; i < seen; i++)
                    Log.Info(
                        $"Player {OwnerClientId} sees {Sights[i].collider.name} on layer {LayerMask.LayerToName(Sights[i].collider.gameObject.layer)} in {Sights[i].distance}m, usable {(Sights[i].collider.GetComponentInParent<IUsable>() == null ? "no" : "yes")}");

                Log.Info($"Player {OwnerClientId} used nothing within {reach}m, saw {seen} colliders");
                return;
            }

            Log.Info($"Player {OwnerClientId} uses {((Component)usable).name}");
            usable.Use(NetworkObject);
        }

        public bool TryLook(float distance, out RaycastHit hit)
        {
            return TryLook(Eyes, movement.Look, distance, transform, out hit);
        }

        public int Look(float distance, RaycastHit[] into)
        {
            return Look(Eyes, movement.Look, distance, transform, into);
        }

        public static bool TryLook(Vector3 origin, Vector3 direction, float distance, Transform looker,
            out RaycastHit hit)
        {
            int found = Look(origin, direction, distance, looker, Sights);
            return TryNearest(Sights, found, out hit);
        }

        public static int Look(Vector3 origin, Vector3 direction, float distance, Transform looker, RaycastHit[] into)
        {
            int found = Physics.RaycastNonAlloc(origin, direction, into, distance, LookMask,
                QueryTriggerInteraction.Ignore);
            int kept = 0;

            for (int i = 0; i < found; i++)
            {
                if (into[i].transform.IsChildOf(looker)) continue;
                into[kept++] = into[i];
            }

            return kept;
        }

        public static bool TryNearest(RaycastHit[] hits, int found, out RaycastHit hit)
        {
            hit = default;
            float closest = float.PositiveInfinity;

            for (int i = 0; i < found; i++)
            {
                if (hits[i].distance >= closest) continue;

                hit = hits[i];
                closest = hits[i].distance;
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
