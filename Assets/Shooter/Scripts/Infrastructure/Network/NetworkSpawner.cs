using System;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Infrastructure
{
    public static class NetworkSpawner
    {
        private static readonly Journal Log = Logs.Here();

        public static GameObject InstantiateAndSpawn(GameObject prefab, Action<GameObject> beforeNetworkSpawn)
        {
            return InstantiateAndSpawn(prefab, Vector3.zero, Quaternion.identity, null, beforeNetworkSpawn);
        }

        public static GameObject InstantiateAndSpawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return InstantiateAndSpawn(prefab, position, rotation, null, null);
        }

        public static GameObject InstantiateAndSpawn(GameObject prefab, Transform parent)
        {
            return InstantiateAndSpawn(prefab, Vector3.zero, Quaternion.identity, parent, null);
        }

        private static GameObject InstantiateAndSpawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> beforeNetworkSpawn)
        {
            Log.Info($"Spawning {prefab.name} at {position} with parent {(parent != null ? parent.name : "None")}...");

            NetworkObject parentNetworkObject = null;
            if (parent != null)
            {
                parentNetworkObject = GetSpawnedNetworkObjectOn(parent);
                if (parentNetworkObject == null)
                {
                    Log.Error($"Parent {parent.name} does not have a spawned network object, {prefab.name} is not spawned");
                    return null;
                }
            }

            GameObject instance = Object.Instantiate(prefab, position, rotation);

            NetworkObject networkObject = instance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Log.Error($"Spawned {prefab.name} does not have a network object, destroying...");
                Object.Destroy(instance);
                return null;
            }

            beforeNetworkSpawn?.Invoke(instance);
            networkObject.Spawn(true);

            if (parentNetworkObject != null && !networkObject.TrySetParent(parentNetworkObject, false))
            {
                Log.Error($"Failed to parent {instance.name} to {parent.name} via Netcode, despawning...");
                networkObject.Despawn(true);
                return null;
            }

            return instance;
        }

        private static NetworkObject GetSpawnedNetworkObjectOn(Transform parent)
        {
            NetworkObject networkObject = parent.GetComponent<NetworkObject>();
            return networkObject != null && networkObject.IsSpawned ? networkObject : null;
        }
    }
}
