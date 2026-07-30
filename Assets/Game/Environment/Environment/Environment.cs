using Shooter.Configuring;
using Shooter.Game.Body;
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
    public class Environment : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static Environment Current { get; private set; }

        [SerializeField] private GameObject corpse;

        [SerializeField] private ItemCatalog items;

        [SerializeField] private SoundCatalog sounds;

        [SerializeField] private SkinCatalog skins;

        private readonly NetworkVariable<FixedString64Bytes> world = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<FixedString32Bytes> version = new NetworkVariable<FixedString32Bytes>();

        public Clock Clock { get; private set; }

        public SleepCycle SleepCycle { get; private set; }

        public GameObject Corpse => corpse;

        public ItemCatalog Items => items;

        public SoundCatalog Sounds => sounds;

        public SkinCatalog Skins => skins;

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
