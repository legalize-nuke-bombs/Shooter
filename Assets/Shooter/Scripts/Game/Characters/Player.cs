using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(CharacterId))]
    public class Player : ShooterBehaviour, ISaveableComponent
    {
        private CharacterId id;

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

        protected override void ShooterAwake()
        {
            id = GetComponent<CharacterId>();
        }

        public long Id => id.Value;

        public static Player Of(long id, Inactive gate)
        {
            foreach (Player player in Registers.Of<Player>(gate))
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
            foreach (Player player in Registers.Of<Player>(gate))
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
