using Shooter.Accounts;
using Shooter.Configuring;
using Shooter.Game.Core;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Playing
{
    public static class OwnPlayer
    {
        private static Account account;
        private static string publicKey;

        public static T Find<T>() where T : Component
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsListening) return null;
            if (Registers.Current == null) return null;

            Player player = network.IsServer ? ByKey() : ByOwnership();
            if (player == null) return null;

            return player.GetComponent<T>();
        }

        private static Player ByKey()
        {
            string key = OwnKey();
            return key == null ? null : Player.OfKey(key, Inactive.Exclude);
        }

        private static Player ByOwnership()
        {
            foreach (Player player in Registers.Current.Of<Player>(Inactive.Exclude))
            {
                NetworkObject net = player.GetComponent<NetworkObject>();
                if (net != null && net.IsOwner) return player;
            }

            return null;
        }

        private static string OwnKey()
        {
            Account current = Config.Account;
            if (current == null) return null;

            if (!ReferenceEquals(current, account))
            {
                account = current;
                publicKey = current.Public;
            }

            return publicKey;
        }
    }
}
