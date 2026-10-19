using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(CharacterId))]
    public class Player : ShooterBehaviour, ISaveableComponent
    {
        private CharacterId characterId;

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
            PublicKey = content.ToObject<SaveData>().PublicKey;
        }

        protected override void ShooterAwake()
        {
            characterId = GetComponent<CharacterId>();
        }

        public long Id => characterId.Value;

        public static Player FindById(long id, RegisterInactivePolicy inactivePolicy)
        {
            foreach (Player player in Registers.GetAll<Player>(inactivePolicy))
            {
                if (player.Id == id)
                {
                    return player;
                }
            }
            return null;
        }

        public static Player FindByPublicKey(string publicKey, RegisterInactivePolicy inactivePolicy)
        {
            foreach (Player player in Registers.GetAll<Player>(inactivePolicy))
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
