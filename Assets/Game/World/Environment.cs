using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Game.Combat;
using Shooter.Game.Llm;
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
    [RequireComponent(typeof(Registers))]
    [RequireComponent(typeof(Clock))]
    [RequireComponent(typeof(SleepCycle))]
    [RequireComponent(typeof(Sweeper))]
    [RequireComponent(typeof(BulletHoles))]
    public class Environment : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static Environment Current { get; private set; }

        [SerializeField] private MainProfiler profiler;
        public MainProfiler Profiler => profiler;

        [SerializeField] private GameObject corpse;

        [SerializeField] private ItemCatalog items;

        [SerializeField] private IconCatalog icons;

        [SerializeField] private NotificationCatalog notifications;

        [SerializeField] private SoundCatalog sounds;

        [SerializeField] private EarSoundCatalog earSounds;

        [SerializeField] private SkinCatalog skins;

        [SerializeField] private NameCatalog names;

        private MainSpawnPoint spawn;

        private readonly NetworkVariable<FixedString64Bytes> world = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<FixedString32Bytes> version = new NetworkVariable<FixedString32Bytes>();

        public Registers Registers { get; private set; }

        public Clock Clock { get; private set; }

        public SleepCycle SleepCycle { get; private set; }

        public Sweeper Sweeper { get; private set; }

        public BulletHoles BulletHoles { get; private set; }

        public Transform Spawn => spawn == null ? transform : spawn.transform;

        public GameObject Corpse => corpse;

        public ItemCatalog Items => items;

        public IconCatalog Icons => icons;

        public NotificationCatalog Notifications => notifications;

        public SoundCatalog Sounds => sounds;

        public EarSoundCatalog EarSounds => earSounds;

        public SkinCatalog Skins => skins;

        public NameCatalog Names => names;

        public string World => world.Value.ToString();

        public string Version => version.Value.ToString();

        private void Awake()
        {
            Registers = GetComponent<Registers>();
            Clock = GetComponent<Clock>();
            SleepCycle = GetComponent<SleepCycle>();
            Sweeper = GetComponent<Sweeper>();
            BulletHoles = GetComponent<BulletHoles>();
            MainSpawnPoint[] points = FindObjectsByType<MainSpawnPoint>();
            spawn = points.Length == 0 ? null : points[0];

            if (items != null) Kinds.Use<UniqueItem>(items);

            if (profiler == null)
                Log.Warn("World has no profiler, nothing will be measured");

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

                Sweeper.enabled = true;
            }

            Log.Info($"Environment is up: world {World}, version {Version}, clock says {Clock.DateTime()}");
        }

        public override void OnNetworkDespawn()
        {
            Sweeper.enabled = false;

            Log.Info("Environment is down");
        }
    }
}
