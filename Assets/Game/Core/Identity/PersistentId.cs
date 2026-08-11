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
            Register<PersistentId> register = Environment.Current.Registers.Of<PersistentId>();

            if (IsServer) value.Value = register.Add(this);
            else if (value.Value != Nobody) register.Add(value.Value, this);

            value.OnValueChanged += Renumbered;
        }

        public override void OnNetworkDespawn()
        {
            value.OnValueChanged -= Renumbered;

            if (Environment.Current != null) Environment.Current.Registers.Of<PersistentId>().Remove(value.Value);
        }

        private void Renumbered(long previous, long current)
        {
            if (Environment.Current == null) return;

            Register<PersistentId> register = Environment.Current.Registers.Of<PersistentId>();

            register.Remove(previous);

            if (current != Nobody) register.Add(current, this);
        }
    }
}
