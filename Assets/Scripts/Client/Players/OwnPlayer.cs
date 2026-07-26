using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Players
{
    public static class OwnPlayer
    {
        public static T Find<T>() where T : Component
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsListening || network.SpawnManager == null) return null;

            NetworkObject player = network.SpawnManager.GetLocalPlayerObject();
            if (player == null) return null;

            return player.GetComponent<T>();
        }
    }
}
