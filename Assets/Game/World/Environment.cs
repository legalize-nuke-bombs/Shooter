using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class Environment : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<FixedString32Bytes> version = new();

        public static Environment Current { get; private set; }

        public string Version => version.Value.ToString();

        private void Awake()
        {
            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
        }

        public override void OnDestroy()
        {
            if (Current == this) Current = null;
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                version.Value = new FixedString32Bytes(Application.version);
            }

            Log.Info($"Environment is up: version {Version}");
        }

        public override void OnNetworkDespawn()
        {
            Log.Info("Environment is down");
        }
    }
}
