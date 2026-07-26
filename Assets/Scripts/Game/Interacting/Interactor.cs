using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Moving;
using Shooter.Logging;

namespace Shooter.Game.Interacting
{
    [RequireComponent(typeof(Movement))]
    public class Interactor : NetworkBehaviour
    {
        public const float EyeHeight = 0.75f;

        [SerializeField] private float reach = 3f;

        private Movement movement;

        public Vector3 Eyes => transform.position + Vector3.up * EyeHeight;

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
            return Physics.Raycast(Eyes, movement.Look, out hit, distance);
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
