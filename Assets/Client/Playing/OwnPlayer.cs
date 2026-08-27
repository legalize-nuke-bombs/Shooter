using Shooter.Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Playing
{
    public static class OwnPlayer
    {
        public static T Find<T>() where T : Component
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsListening || network.SpawnManager == null) return null;

            NetworkObject player = network.SpawnManager.GetLocalPlayerObject() ?? Owned();
            if (player == null) return null;

            return player.GetComponent<T>();
        }

        private static NetworkObject Owned()
        {
            if (Registers.Current == null) return null;

            foreach (Player player in Registers.Current.Of<Player>(Inactive.Include))
            {
                NetworkObject net = player.GetComponent<NetworkObject>();
                if (net != null && net.IsOwner) return net;
            }

            return null;
        }
    }
}
