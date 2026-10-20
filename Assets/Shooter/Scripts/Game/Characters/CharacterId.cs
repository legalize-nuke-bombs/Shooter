using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class CharacterId : ShooterNetworkBehaviour, ISaveableComponent
    {
        public const long None = -1;

        private readonly NetworkVariable<long> id = new(None);

        public long Value => id.Value;

        public string ComponentKey => "CharacterId";

        private struct SaveData
        {
            public long Id { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Id = Value
            };
        }

        public void LoadObject(SaveToken content)
        {
            id.Value = content.ToObject<SaveData>().Id;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer || id.Value != None) return;

            CharacterIds characterIds = Registers.GetAll<CharacterIds>(RegisterInactivePolicy.Include).FirstOrDefault();
            id.Value = characterIds == null ? Random.Range(0, int.MaxValue) : characterIds.TakeNextId();
        }
    }
}
