using System;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Core
{
    public static class Spawner
    {
        private static readonly Journal Log = Logs.Here();

        public static GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        }

        public static GameObject Spawn(GameObject prefab, Action<GameObject> prepare)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null, prepare);
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Spawn(prefab, position, rotation, null);
        }

        public static GameObject Spawn(GameObject prefab, Transform parent)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            return Spawn(prefab, position, rotation, parent, null);
        }

        // prepare runs between Instantiate and the network spawn: the one moment to set what OnNetworkSpawn will read
        private static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent,
            Action<GameObject> prepare)
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

            GameObject body = UnityEngine.Object.Instantiate(prefab, position, rotation);

            NetworkObject networkObject = body.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Log.Error($"Spawned {prefab.name} does not have a network object, destroying...");
                UnityEngine.Object.Destroy(body);
                return null;
            }

            prepare?.Invoke(body);
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
