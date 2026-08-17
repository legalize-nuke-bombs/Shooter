using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Game.Combat;
using Shooter.Game.Notifying;
using Shooter.Game.Core;
using Shooter.Game.Loot;
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

        public static Environment Current { get; private set; }

        [SerializeField] private GameObject corpse;

        private MainSpawnPoint spawn;

        private readonly NetworkVariable<FixedString64Bytes> world = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<FixedString32Bytes> version = new NetworkVariable<FixedString32Bytes>();

        public Transform Spawn => spawn == null ? transform : spawn.transform;

        public GameObject Corpse => corpse;

        public string World => world.Value.ToString();

        public string Version => version.Value.ToString();

        private void Awake()
        {
            MainSpawnPoint[] points = FindObjectsByType<MainSpawnPoint>();
            spawn = points.Length == 0 ? null : points[0];

            ItemCatalog items = Catalogs.Of<ItemCatalog>();
            if (items != null) Kinds.Use<UniqueItem>(items);

            if (spawn == null)
                Log.Warn($"World has no main spawn point, everyone will appear at {transform.position}");
            else if (points.Length > 1)
                Log.Warn($"World has {points.Length} main spawn points, everyone will appear at the one on {spawn.name}");

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
