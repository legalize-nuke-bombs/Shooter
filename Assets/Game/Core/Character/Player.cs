using Shooter.Game.Core.Saves;
using UnityEngine;

namespace Shooter.Game.Core
{
    [RequireComponent(typeof(GameObjectRuntimeId))]
    public class Player : RegisteredBehaviour, ISaveableComponent
    {
        private GameObjectRuntimeId id;

        public string PublicKey { get; set; }

        public string ComponentKey => "Player";

        private struct SaveData
        {
            public string PublicKey { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData { PublicKey = PublicKey };
        }

        public void LoadObject(SaveToken content)
        {
            PublicKey = content.To<SaveData>().PublicKey;
        }

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

        public static Player OfKey(string publicKey, Inactive gate)
        {
            foreach (Player player in Registers.Current.Of<Player>(gate))
            {
                if (player.PublicKey == publicKey)
                {
                    return player;
                }
            }
            return null;
        }
    }
}
