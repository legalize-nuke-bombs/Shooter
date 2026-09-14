using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Core
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class Spawner : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static Spawner Current { get; private set; }

        private void Awake()
        {
            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Spawn(prefab, position, rotation, null);
        }

        public GameObject Spawn(GameObject prefab, Transform parent)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            Log.Info($"Spawning {prefab.name} at {position} with parent {(parent != null ? parent.name : "None")}...");

            // Netcode parents only under a spawned network object sitting on this very transform;
            // GetComponent (unlike GetComponentInParent) also finds it on a frozen, inactive parent
            NetworkObject parentNetworkObject = null;
            if (parent != null)
            {
                parentNetworkObject = parent.GetComponent<NetworkObject>();
                if (parentNetworkObject == null || !parentNetworkObject.IsSpawned)
                {
                    Log.Error($"Parent {parent.name} does not have a spawned network object, {prefab.name} is not spawned");
                    return null;
                }
            }

            GameObject body = Instantiate(prefab, position, rotation);

            NetworkObject networkObject = body.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Log.Error($"Spawned {prefab.name} does not have a network object, destroying...");
                Destroy(body);
                return null;
            }

            networkObject.Spawn(true);

            if (parentNetworkObject != null && !networkObject.TrySetParent(parentNetworkObject, false))
            {
                Log.Error($"Failed to parent {body.name} to {parent.name} via Netcode, despawning...");
                networkObject.Despawn(true);
                return null;
            }

            return body;
        }
    }
}
