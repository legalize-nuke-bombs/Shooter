using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    public class HostVersion : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<FixedString32Bytes> version = new();

        public string Version => version.Value.ToString();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                version.Value = new FixedString32Bytes(Application.version);
            }

            Log.Info($"The world is up: host version {Version}");
        }

        public override void OnNetworkDespawn()
        {
            Log.Info("The world is down");
        }
    }
}
