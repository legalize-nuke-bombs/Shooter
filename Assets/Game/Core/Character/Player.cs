using UnityEngine;

namespace Shooter.Game.Core
{
    [RequireComponent(typeof(GameObjectRuntimeId))]
    public class Player : RegisteredBehaviour
    {
        private GameObjectRuntimeId id;

        private void Awake()
        {
            id = GetComponent<GameObjectRuntimeId>();
        }

        public long Id => id.Value;

        public static Player Of(long id)
        {
            foreach (Player player in Registers.Current.Of<Player>())
            {
                if (player.Id == id)
                {
                    return player;
                }

            }
            return null;
        }
    }
}
