using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Shooter.Configuring;
using Shooter.Game.Items;
using Shooter.Game.Sleeping;
using Shooter.Game.Sounding;
using Shooter.Game.Timing;
using Shooter.Logging;

namespace Shooter.Game
{
    [RequireComponent(typeof(Clock))]
    [RequireComponent(typeof(SleepCycle))]
    public class Environment : NetworkBehaviour
    {
        public static Environment Current { get; private set; }

        [SerializeField] private GameObject corpse;

        [SerializeField] private ItemCatalog items;

        [SerializeField] private SoundCatalog sounds;

        private readonly NetworkVariable<FixedString64Bytes> world = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<FixedString32Bytes> version = new NetworkVariable<FixedString32Bytes>();

        public Clock Clock { get; private set; }

        public SleepCycle SleepCycle { get; private set; }

        public GameObject Corpse => corpse;

        public ItemCatalog Items => items;

        public SoundCatalog Sounds => sounds;

        public string World => world.Value.ToString();

        public string Version => version.Value.ToString();

        private void Awake()
        {
            Clock = GetComponent<Clock>();
            SleepCycle = GetComponent<SleepCycle>();
        }

        public override void OnNetworkSpawn()
        {
            Current = this;

            if (IsServer)
            {
                ServerConfig config = Config.Read<ServerConfig>(ServerConfig.FileName);
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
