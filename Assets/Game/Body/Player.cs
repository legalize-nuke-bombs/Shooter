using Shooter.Game.Core;
using Unity.Netcode;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Body
{
    public class Player : NetworkBehaviour
    {
        private long registered;

        public override void OnNetworkSpawn()
        {
            if (IsServer) registered = Environment.Current.Registers.Of<Player>().Add(this);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            Environment world = Environment.Current;
            if (world != null) world.Registers.Of<Player>().Remove(registered);
        }
    }
}
