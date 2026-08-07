using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Game.Body.EarSounding;
using Shooter.Game.Body.Sounding;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Clock))]
    [RequireComponent(typeof(SleepCycle))]
    [RequireComponent(typeof(UniqueItemIdProvider))]
    public class Environment : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static Environment Current { get; private set; }

        [SerializeField] private GameObject corpse;

        [SerializeField] private ItemCatalog items;

        [SerializeField] private SoundCatalog sounds;

        [SerializeField] private EarSoundCatalog earSounds;

        [SerializeField] private SkinCatalog skins;

        [SerializeField] private NameCatalog names;

        private MainSpawnPoint spawn;

        private readonly NetworkVariable<FixedString64Bytes> world = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<FixedString32Bytes> version = new NetworkVariable<FixedString32Bytes>();

        public Clock Clock { get; private set; }

        public SleepCycle SleepCycle { get; private set; }

        public UniqueItemIdProvider ItemIds { get; private set; }

        public Transform Spawn => spawn == null ? transform : spawn.transform;

        public GameObject Corpse => corpse;

        public ItemCatalog Items => items;

        public SoundCatalog Sounds => sounds;

        public EarSoundCatalog EarSounds => earSounds;

        public SkinCatalog Skins => skins;

        public NameCatalog Names => names;

        public string World => world.Value.ToString();

        public string Version => version.Value.ToString();

        private void Awake()
        {
            Clock = GetComponent<Clock>();
            SleepCycle = GetComponent<SleepCycle>();
            ItemIds = GetComponent<UniqueItemIdProvider>();
            MainSpawnPoint[] points = FindObjectsByType<MainSpawnPoint>();
            spawn = points.Length == 0 ? null : points[0];

            if (spawn == null)
                Log.Warn("World has no main spawn point, everyone will appear at {}", transform.position);
            else if (points.Length > 1)
                Log.Warn("World has {} main spawn points, everyone will appear at the one on {}", points.Length, spawn.name);
        }

        public override void OnNetworkSpawn()
        {
            Current = this;

            if (IsServer)
            {
                ServerConfig config = Config.Read().Server;
                world.Value = new FixedString64Bytes(config.World);
                version.Value = new FixedString32Bytes(Application.version);
            }

            Log.Info("Environment is up: world {}, version {}, clock says {}", World, Version, Clock.DateTime());
        }

        public override void OnNetworkDespawn()
        {
            if (Current != this) return;

            Current = null;
            Log.Info("Environment is down");
        }
    }
}
