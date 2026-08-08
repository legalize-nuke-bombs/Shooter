using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Game.Body.EarSounding;
using Shooter.Game.Identity;
using Shooter.Game.Body.Sounding;
using Shooter.Game.Loot;
using Shooter.Game.Sweeping;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PersistentIdProvider))]
    [RequireComponent(typeof(Clock))]
    [RequireComponent(typeof(SleepCycle))]
    [RequireComponent(typeof(Sweeper))]
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

        public PersistentIdProvider PersistentIdProvider { get; private set; }

        public Clock Clock { get; private set; }

        public SleepCycle SleepCycle { get; private set; }

        public Sweeper Sweeper { get; private set; }

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
            PersistentIdProvider = GetComponent<PersistentIdProvider>();
            Clock = GetComponent<Clock>();
            SleepCycle = GetComponent<SleepCycle>();
            Sweeper = GetComponent<Sweeper>();
            MainSpawnPoint[] points = FindObjectsByType<MainSpawnPoint>();
            spawn = points.Length == 0 ? null : points[0];

            if (spawn == null)
                Log.Warn("World has no main spawn point, everyone will appear at {}", transform.position);
            else if (points.Length > 1)
                Log.Warn("World has {} main spawn points, everyone will appear at the one on {}", points.Length, spawn.name);

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

            Log.Info("Environment is up: world {}, version {}, clock says {}", World, Version, Clock.DateTime());
        }

        public override void OnNetworkDespawn()
        {
            Sweeper.enabled = false;

            Log.Info("Environment is down");
        }
    }
}
