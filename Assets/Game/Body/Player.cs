using Shooter.Game.Core;
using Unity.Netcode;

namespace Shooter.Game.Body
{
    public class Player : NetworkBehaviour
    {
        private long registered;

        public override void OnNetworkSpawn()
        {
            if (IsServer) registered = Registers.Current.Of<Player>().Add(this);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            Registers world = Registers.Current;
            if (world != null) world.Of<Player>().Remove(registered);
        }
    }
}
