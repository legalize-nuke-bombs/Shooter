using Shooter.Logging;
using Unity.Netcode;

namespace Shooter.Game.Core
{
    public abstract class RegisteredNetworkBehaviour : NetworkBehaviour, IRegistered
    {
        private static readonly Journal Log = Logs.Here();

        public override void OnNetworkSpawn()
        {
            Log.Info($"Entity {name} is tracking");
            Registers.Track(this);
        }

        public override void OnNetworkDespawn()
        {
            Log.Info($"Entity {name} is untracking");
            Registers.Untrack(this);
        }
    }
}
