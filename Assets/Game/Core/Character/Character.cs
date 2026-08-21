using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;
using Unity.Netcode;

namespace Shooter.Game.Core
{
    public class Character : RegisteredNetworkBehaviour, ISaveableComponent
    {
        public const long Nobody = -1;

        private readonly NetworkVariable<long> value = new(Nobody);

        public long Value => value.Value;

        public static Character Of(long id)
        {
            Registers world = Registers.Current;
            if (world == null) return null;

            foreach (Character character in world.Of<Character>())
                if (character.Value == id)
                    return character;

            return null;
        }

        public string ComponentKey => "Character";
        private struct SaveData
        {
            public long Id { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Id = value.Value
            };
        }
        public void LoadObject(SaveToken content)
        {
            value.Value = content.To<SaveData>().Id;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer) value.Value = CharacterIds.Current.Next();
        }
    }
}
