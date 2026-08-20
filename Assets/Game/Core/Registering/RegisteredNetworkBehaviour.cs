using Unity.Netcode;

namespace Shooter.Game.Core
{
    public abstract class RegisteredNetworkBehaviour : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            Registers.Track(this);
        }

        public override void OnNetworkDespawn()
        {
            Registers.Untrack(this);
        }
    }
}
