using Unity.Netcode;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Core
{
    public class PersistentId : NetworkBehaviour
    {
        public const long Nobody = -1;

        private readonly NetworkVariable<long> value = new NetworkVariable<long>(Nobody);

        public long Value => value.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer) value.Value = Registers.Current.Of<PersistentId>().Add(this);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            if (Registers.Current != null) Registers.Current.Of<PersistentId>().Remove(value.Value);
        }
    }
}
