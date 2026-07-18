using UnityEngine;
using Shooter.Client.Aiming;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds.Entities.Players;
using Shooter.Server.Worlds.Entities.Sleeping;
using Shooter.Server.Worlds;

namespace Shooter.Client.Sleeping
{
    [RequireComponent(typeof(Aim))]
    public class SleepSense : MonoBehaviour
    {
        public bool MySleeping { get; private set; }
        public bool WorldAsleep { get; private set; }
        public bool CanSleep => !MySleeping && night && aim.BedDistance <= Sleep.UseReach;

        private Aim aim;
        private bool night;
        private NetworkClient networkClient;
        private bool netHooked;

        private void Awake()
        {
            aim = GetComponent<Aim>();
        }

        private void OnDisable()
        {
            if (netHooked)
                networkClient.SnapshotReceived -= OnSnapshot;
            netHooked = false;
        }

        private void Update()
        {
            if (netHooked || NetworkClient.Instance == null) return;
            networkClient = NetworkClient.Instance;
            networkClient.SnapshotReceived += OnSnapshot;
            netHooked = true;
        }

        private void OnSnapshot(Snapshot snapshot)
        {
            night = ClockState.IsNight(snapshot.Clock.Fraction());
            WorldAsleep = snapshot.Sleep.WorldAsleep;
            foreach (PlayerState state in snapshot.Players)
            {
                if (state.Id != networkClient.PlayerId) continue;
                MySleeping = state.Sleeping;
                break;
            }
        }
    }
}
