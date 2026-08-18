using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    [RequireComponent(typeof(SaveableObject))]
    public class NetworkSaveableObject : NetworkBehaviour
    {
        private SaveableObject saveableObject;

        private void Awake()
        {
            saveableObject = GetComponent<SaveableObject>();
        }

        public override void OnNetworkSpawn()
        {
            saveableObject.Spawned = true;
        }

        public override void OnNetworkDespawn()
        {
            saveableObject.Spawned = false;
        }
    }
}
