using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class CharacterId : NetworkBehaviour, ISaveableComponent
    {
        public const long None = -1;

        private readonly NetworkVariable<long> value = new(None);

        public long Value => value.Value;

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
            value.Value = content.To<SaveData>().Id;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer || value.Value != None) return;

            CharacterIds ids = Registers.Of<CharacterIds>(Inactive.Include).FirstOrDefault();
            value.Value = ids == null ? Random.Range(0, int.MaxValue) : ids.Next();
        }
    }
}
