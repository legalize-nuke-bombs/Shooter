using System;
using Unity.Netcode;

namespace Shooter.Game
{
    public class Clock : NetworkBehaviour
    {
        public const double DawnFraction = 0.25;
        public const double DuskFraction = 0.75;

        private const long DayLengthSeconds = 86400;
        private const float DayRealSeconds = 600f;
        private const float SyncInterval = 1f;

        private static readonly DateTimeOffset Beginning = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

        private readonly NetworkVariable<double> synced = new NetworkVariable<double>();
        private readonly NetworkVariable<float> scale = new NetworkVariable<float>(1f);

        private double timestamp;
        private float untilSync;

        public double Timestamp => timestamp;

        public DateTimeOffset Now => Beginning.AddSeconds(timestamp);

        public double DayFraction => Now.TimeOfDay.TotalSeconds / DayLengthSeconds;

        public float Scale
        {
            get => scale.Value;
            set
            {
                if (IsServer) scale.Value = value;
            }
        }

        public override void OnNetworkSpawn()
        {
            timestamp = synced.Value;
            synced.OnValueChanged += Adjust;
            NetworkManager.NetworkTickSystem.Tick += Step;
        }

        public override void OnNetworkDespawn()
        {
            synced.OnValueChanged -= Adjust;
            NetworkManager.NetworkTickSystem.Tick -= Step;
        }

        public bool IsNight()
        {
            double fraction = DayFraction;

            return fraction >= DuskFraction || fraction < DawnFraction;
        }

        public string DateTime()
        {
            return Now.ToString("yyyy.MM.dd HH:mm:ss");
        }

        private void Step()
        {
            float dt = NetworkManager.LocalTime.FixedDeltaTime;
            timestamp += dt * scale.Value * (DayLengthSeconds / DayRealSeconds);

            if (!IsServer) return;

            untilSync -= dt;
            if (untilSync > 0f) return;

            untilSync = SyncInterval;
            synced.Value = timestamp;
        }

        private void Adjust(double previous, double current)
        {
            if (IsServer) return;

            timestamp = current;
        }
    }
}
