using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public static class OwnPlayer
    {
        public static T FindComponent<T>() where T : Component
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsListening) return null;

            foreach (Player player in Registers.GetAll<Player>(RegisterInactivePolicy.Exclude))
            {
                if (player.IsControlledLocally) return player.GetComponent<T>();
            }

            return null;
        }
    }
}
