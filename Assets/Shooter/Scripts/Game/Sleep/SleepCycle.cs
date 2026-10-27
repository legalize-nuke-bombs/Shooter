using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class SleepCycle : ShooterNetworkBehaviour
    {
        private const float SkipTimeScale = 300f;
        private const double BedtimeStartHour = 22;
        private const double BedtimeEndHour = 8;
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<bool> worldAsleep = new();

        private Clock clock;
        private bool wasBedtime;

        public bool IsWorldAsleep => worldAsleep.Value;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            clock = Registers.Instance.Get<Clock>();
            NetworkManager.NetworkTickSystem.Tick += HandleNetworkTick;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick -= HandleNetworkTick;
        }

        private void HandleNetworkTick()
        {
            bool allPlayersAsleep = AreAllPlayersAsleep();
            if (allPlayersAsleep != worldAsleep.Value)
            {
                worldAsleep.Value = allPlayersAsleep;
                clock.Scale = allPlayersAsleep ? SkipTimeScale : 1f;
                Log.Info($"World asleep is now {allPlayersAsleep}");
            }

            if (IsBedtime())
            {
                wasBedtime = true;
                return;
            }

            if (!wasBedtime) return;

            wasBedtime = false;
            Log.Info("Bedtime is over, waking sleepers");
            WakeAllPlayers();
        }

        public bool IsBedtime()
        {
            double hourOfDay = clock.Now.TimeOfDay.TotalHours;

            return hourOfDay >= BedtimeStartHour || hourOfDay < BedtimeEndHour;
        }

        private bool AreAllPlayersAsleep()
        {
            bool anyPlayer = false;
            foreach (Player player in Registers.Instance.GetAll<Player>(RegisterInactivePolicy.Exclude))
            {
                Sleeper sleeper = player.GetComponent<Sleeper>();
                if (sleeper == null || !sleeper.IsSleeping) return false;

                anyPlayer = true;
            }

            return anyPlayer;
        }

        private void WakeAllPlayers()
        {
            foreach (Player player in Registers.Instance.GetAll<Player>(RegisterInactivePolicy.Exclude))
            {
                player.GetComponent<Sleeper>()?.WakeUp();
            }
        }
    }
}
