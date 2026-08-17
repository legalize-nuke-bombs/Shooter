using Shooter.Configuring;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    [DefaultExecutionOrder(-100)]
    public class Environment : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();
        private readonly NetworkVariable<FixedString32Bytes> version = new();

        private readonly NetworkVariable<FixedString64Bytes> world = new();

        public static Environment Current { get; private set; }

        public string World => world.Value.ToString();

        public string Version => version.Value.ToString();

        private void Awake()
        {
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
                ServerConfig config = Config.Read().Server;
                world.Value = new FixedString64Bytes(config.World);
                version.Value = new FixedString32Bytes(Application.version);
            }

            Log.Info($"Environment is up: world {World}, version {Version}, clock says {Clock.Current.DateTime()}");
        }

        public override void OnNetworkDespawn()
        {
            Log.Info("Environment is down");
        }
    }
}
