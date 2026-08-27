using UnityEngine;

namespace Shooter.Game.Core
{
    [RequireComponent(typeof(GameObjectRuntimeId))]
    public class Player : RegisteredBehaviour
    {
        private GameObjectRuntimeId id;

        protected override void Awake()
        {
            base.Awake();
            id = GetComponent<GameObjectRuntimeId>();
        }

        public long Id => id.Value;

        public static Player Of(long id, Inactive gate)
        {
            foreach (Player player in Registers.Current.Of<Player>(gate))
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
