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

            GameObject body = Instantiate(prefab, position, rotation);

            NetworkObject networkObject = body.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Log.Error($"Spawned {prefab.name} does not have a network object, destroying...");
                Destroy(body);
                return null;
            }

            networkObject.Spawn(true);

            if (parent != null)
            {
                NetworkObject parentNetObj = parent.GetComponentInParent<NetworkObject>();
                if (parentNetObj != null && parentNetObj.IsSpawned)
                {
                    bool success = networkObject.TrySetParent(parent, false);

                    if (!success)
                    {
                        Log.Error($"Failed to parent {body.name} to {parent.name} via Netcode!");
                    }
                }
                else
                {
                    Log.Warn($"Parent {parent.name} does not have a spawned NetworkObject. Cannot attach child in Netcode!");
                }
            }

            return body;
        }
    }
}
