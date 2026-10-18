using System;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shooter.Infrastructure
{
    public static class Spawner
    {
        private static readonly Journal Log = Logs.Here();

        public static GameObject Spawn(GameObject prefab, Action<GameObject> beforeNetworkSpawn)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null, beforeNetworkSpawn);
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Spawn(prefab, position, rotation, null, null);
        }

        public static GameObject Spawn(GameObject prefab, Transform parent)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, parent, null);
        }

        private static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> beforeNetworkSpawn)
        {
            Log.Info($"Spawning {prefab.name} at {position} with parent {(parent != null ? parent.name : "None")}...");

            NetworkObject parentNetworkObject = null;
            if (parent != null)
            {
                parentNetworkObject = SpawnedNetworkObjectOn(parent);
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

        private static NetworkObject SpawnedNetworkObjectOn(Transform parent)
        {
            NetworkObject networkObject = parent.GetComponent<NetworkObject>();
            return networkObject != null && networkObject.IsSpawned ? networkObject : null;
        }
    }
}
