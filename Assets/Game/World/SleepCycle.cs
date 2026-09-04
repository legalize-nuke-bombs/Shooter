using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;

namespace Shooter.Game.World
{
    public class SleepCycle : NetworkBehaviour
    {
        private const float SkipTimeScale = 50f;
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<bool> asleep = new();

        private bool wasNight;

        public static SleepCycle Current { get; private set; }

        public bool WorldAsleep => asleep.Value;

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
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick += Step;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick -= Step;
        }

        private void Step()
        {
            Clock clock = Clock.Current;

            bool everyone = AllAsleep();
            if (everyone != asleep.Value)
            {
                asleep.Value = everyone;
                clock.Scale = everyone ? SkipTimeScale : 1f;
                Log.Info($"World asleep is now {everyone}");
            }

            if (clock.IsNight())
            {
                wasNight = true;
                return;
            }

            if (!wasNight) return;

            wasNight = false;
            Log.Info("Dawn broke, waking sleepers");
            WakeAll();
        }

        private bool AllAsleep()
        {
            bool anyone = false;
            foreach (Player player in Registers.Current.Of<Player>(Inactive.Exclude))
            {
                Sleeper sleeper = player.GetComponent<Sleeper>();
                if (sleeper == null || !sleeper.Sleeping) return false;

                anyone = true;
            }

            return anyone;
        }

        private void WakeAll()
        {
            foreach (Player player in Registers.Current.Of<Player>(Inactive.Exclude))
                player.GetComponent<Sleeper>()?.WakeUp();
        }
    }
}
