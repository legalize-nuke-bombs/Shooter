using Unity.Netcode;
using Shooter.Game.Time;
using Shooter.Logging;

namespace Shooter.Game.Sleeping
{
    public class SleepCycle : NetworkBehaviour
    {
        private const float SkipTimeScale = 6f;

        private readonly NetworkVariable<bool> asleep = new NetworkVariable<bool>();

        private Clock clock;
        private bool wasNight;

        public bool WorldAsleep => asleep.Value;

        private void Awake()
        {
            clock = GetComponent<Clock>();
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
            bool everyone = AllAsleep();
            if (everyone != asleep.Value)
            {
                asleep.Value = everyone;
                Log.Info("World asleep is now {}", everyone);
            }

            clock.Scale = everyone ? SkipTimeScale : 1f;

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
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                NetworkObject player = client.PlayerObject;
                if (player == null) continue;

                var sleeper = player.GetComponent<Sleeper>();
                if (sleeper == null || !sleeper.Sleeping) return false;

                anyone = true;
            }

            return anyone;
        }

        private void WakeAll()
        {
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
                client.PlayerObject?.GetComponent<Sleeper>()?.WakeUp();
        }
    }
}
