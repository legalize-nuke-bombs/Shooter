using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public static class OwnPlayer
    {
        private static Account cachedAccount;
        private static string cachedPublicKey;

        public static T FindComponent<T>() where T : Component
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsListening) return null;

            Player player = network.IsServer ? FindByOwnPublicKey() : FindByOwnership();
            if (player == null) return null;

            return player.GetComponent<T>();
        }

        private static Player FindByOwnPublicKey()
        {
            string ownPublicKey = GetOwnPublicKey();
            return ownPublicKey == null ? null : Player.FindByPublicKey(ownPublicKey, RegisterInactivePolicy.Exclude);
        }

        private static Player FindByOwnership()
        {
            foreach (Player player in Registers.GetAll<Player>(RegisterInactivePolicy.Exclude))
            {
                NetworkObject networkObject = player.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsOwner) return player;
            }

            return null;
        }

        private static string GetOwnPublicKey()
        {
            Account currentAccount = Config.Account;
            if (currentAccount == null) return null;

            if (!ReferenceEquals(currentAccount, cachedAccount))
            {
                cachedAccount = currentAccount;
                cachedPublicKey = currentAccount.PublicKey;
            }

            return cachedPublicKey;
        }
    }
}
