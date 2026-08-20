using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;
using Unity.Netcode;

namespace Shooter.Game.Core
{
    public class Character : RegisteredNetworkBehaviour, ISaveableComponent, IIdentified
    {
        public const long Nobody = -1;

        private readonly NetworkVariable<long> value = new(Nobody);

        public long Value => value.Value;

        long IIdentified.Id => value.Value;

        public string ComponentKey => "Character";
        private struct SaveData
        {
            public long Id { get; set; }
        }
        public object SaveComponent()
        {
            return new SaveData
            {
                Id = value.Value
            };
        }
        public void LoadComponent(JToken content)
        {
            value.Value = content.ToObject<SaveData>().Id;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer) value.Value = CharacterIds.Current.Next();
        }
    }
}
