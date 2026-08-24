using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Core
{
    public class Spawner : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static Spawner Current { get; private set; }

        private void Awake()
        {
            Current = this;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position = new Vector3(), Quaternion rotation = new Quaternion())
        {
            Log.Info($"Spawning {prefab.name} at {position} rotation {rotation}...");
            GameObject body = Instantiate(prefab, position, rotation);

            NetworkObject networkObject = body.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Log.Error($"Spawned {prefab.name} does not have a network object, destroying...");
                Destroy(body);
                return null;
            }
            networkObject.Spawn(true);

            return body;
        }
    }
}
