using Unity.Netcode;

namespace Shooter.Game.Identity
{
    public class PersistentId : NetworkBehaviour
    {
        private readonly NetworkVariable<long> value = new NetworkVariable<long>();
        public long Value => value.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                value.Value = Environment.Current.PersistentIdProvider.Reserve();
            }
        }
    }
}
