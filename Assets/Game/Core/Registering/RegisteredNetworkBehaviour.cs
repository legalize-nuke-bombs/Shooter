using Unity.Netcode;

namespace Shooter.Game.Core
{
    public abstract class RegisteredNetworkBehaviour : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            Registers.Current.Track(this);
        }

        public override void OnNetworkDespawn()
        {
            Registers world = Registers.Current;
            if (world != null) world.Untrack(this);
        }
    }
}
