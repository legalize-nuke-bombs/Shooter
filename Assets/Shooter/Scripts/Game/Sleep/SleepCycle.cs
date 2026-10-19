using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class SleepCycle : ShooterNetworkBehaviour
    {
        private const float SkipTimeScale = 300f;
        private const double BedtimeHours = 22;
        private const double WakeHours = 8;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly NetworkVariable<bool> asleep = new();

        private Clock clock;
        private bool wasBedtime;

        public bool WorldAsleep => asleep.Value;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            clock = Registers.GetAll<Clock>(RegisterInactivePolicy.Include).Single();
            NetworkManager.NetworkTickSystem.Tick += Step;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick -= Step;
        }

        private void Step()
        {
            bool allAsleep = AreAllPlayersAsleep();
            if (allAsleep != asleep.Value)
            {
                asleep.Value = allAsleep;
                clock.Scale = allAsleep ? SkipTimeScale : 1f;
                Log.Info($"World asleep is now {allAsleep}");
            }

            if (IsBedtime())
            {
                wasBedtime = true;
                return;
            }

            if (!wasBedtime) return;

            wasBedtime = false;
            Log.Info("Bedtime is over, waking sleepers");
            WakeAll();
        }

        public bool IsBedtime()
        {
            double hours = clock.Now.TimeOfDay.TotalHours;

            return hours >= BedtimeHours || hours < WakeHours;
        }

        private bool AreAllPlayersAsleep()
        {
            bool anyone = false;
            foreach (Player player in Registers.GetAll<Player>(RegisterInactivePolicy.Exclude))
            {
                Sleeper sleeper = player.GetComponent<Sleeper>();
                if (sleeper == null || !sleeper.Sleeping) return false;

                anyone = true;
            }

            return anyone;
        }

        private void WakeAll()
        {
            foreach (Player player in Registers.GetAll<Player>(RegisterInactivePolicy.Exclude))
            {
                player.GetComponent<Sleeper>()?.WakeUp();
            }
        }
    }
}
