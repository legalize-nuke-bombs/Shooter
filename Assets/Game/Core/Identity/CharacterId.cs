using Unity.Netcode;

namespace Shooter.Game.Core
{
    public class CharacterId : NetworkBehaviour
    {
        public const long Nobody = -1;

        private readonly NetworkVariable<long> value = new(Nobody);

        public long Value => value.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer) value.Value = Registers.Current.Of<CharacterId>().Add(this);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            if (Registers.Current != null) Registers.Current.Of<CharacterId>().Remove(value.Value);
        }
    }
}
