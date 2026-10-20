using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public static class LocalPlayerFinder
    {
        public static T FindComponent<T>() where T : Component
        {
            foreach (PlayerController playerController in Registers.GetAll<PlayerController>(RegisterInactivePolicy.Exclude))
            {
                if (playerController.IsControlledLocally) return playerController.GetComponent<T>();
            }

            return null;
        }
    }
}
