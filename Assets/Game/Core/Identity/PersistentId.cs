using Unity.Netcode;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Core
{
    public class PersistentId : NetworkBehaviour
    {
        private readonly NetworkVariable<long> value = new NetworkVariable<long>(PersistentIds.Nobody);

        public long Value => value.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                value.Value = Environment.Current.PersistentIds.Reserve();
            }

            value.OnValueChanged += Renumbered;

            if (value.Value != PersistentIds.Nobody) Environment.Current.PersistentIds.Register(this);
        }

        public override void OnNetworkDespawn()
        {
            value.OnValueChanged -= Renumbered;

            if (Environment.Current != null) Environment.Current.PersistentIds.Forget(value.Value, this);
        }

        private void Renumbered(long previous, long current)
        {
            if (Environment.Current == null) return;

            Environment.Current.PersistentIds.Forget(previous, this);

            if (current != PersistentIds.Nobody) Environment.Current.PersistentIds.Register(this);
        }
    }
}
