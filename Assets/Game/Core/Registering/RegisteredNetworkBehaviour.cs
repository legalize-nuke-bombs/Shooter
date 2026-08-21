using Shooter.Logging;
using Unity.Netcode;

namespace Shooter.Game.Core
{
    public abstract class RegisteredNetworkBehaviour : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public override void OnNetworkSpawn()
        {
            Registers registers = Registers.Current;
            if (registers == null)
            {
                Log.Info($"Entity {name} will not be tracked because registers is not set");
                return;
            }
            Log.Info($"Entity {name} is tracking");
            Registers.Current.Track(this);
        }

        public override void OnNetworkDespawn()
        {
            Registers registers = Registers.Current;
            if (registers == null)
            {
                Log.Info($"Entity {name} will not be untracked because registers is not set");
                return;
            }
            Log.Info($"Entity {name} is untracking");
            Registers.Current.Untrack(this);
        }
    }
}
