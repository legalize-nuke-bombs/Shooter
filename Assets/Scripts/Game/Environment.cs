using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Sleeping;
using Shooter.Game.Time;
using Shooter.Logging;

namespace Shooter.Game
{
    [RequireComponent(typeof(Clock))]
    [RequireComponent(typeof(SleepCycle))]
    public class Environment : NetworkBehaviour
    {
        public static Environment Current { get; private set; }

        public Clock Clock { get; private set; }

        public SleepCycle SleepCycle { get; private set; }

        private void Awake()
        {
            Clock = GetComponent<Clock>();
            SleepCycle = GetComponent<SleepCycle>();
        }

        public override void OnNetworkSpawn()
        {
            Current = this;
            Log.Info("Environment is up, clock says {}", Clock.DateTime());
        }

        public override void OnNetworkDespawn()
        {
            if (Current != this) return;

            Current = null;
            Log.Info("Environment is down");
        }
    }
}
