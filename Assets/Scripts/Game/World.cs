using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Sleeping;
using Shooter.Game.Time;
using Shooter.Logging;

namespace Shooter.Game
{
    [RequireComponent(typeof(WorldClock))]
    [RequireComponent(typeof(WorldSleep))]
    public class World : NetworkBehaviour
    {
        public static World Current { get; private set; }

        public WorldClock Clock { get; private set; }

        public WorldSleep Sleep { get; private set; }

        private void Awake()
        {
            Clock = GetComponent<WorldClock>();
            Sleep = GetComponent<WorldSleep>();
        }

        public override void OnNetworkSpawn()
        {
            Current = this;
            Log.Info("World is up, clock says {}", Clock.DateTime());
        }

        public override void OnNetworkDespawn()
        {
            if (Current != this) return;

            Current = null;
            Log.Info("World is down");
        }
    }
}
