using System;
using Unity.Netcode;

namespace Shooter.Game.Time
{
    public class Clock : NetworkBehaviour
    {
        private const float DayRealSeconds = 120f;
        private const float SyncInterval = 1f;
        private const long Delta2026 = 1767225600L;

        private readonly NetworkVariable<double> synced = new NetworkVariable<double>();
        private readonly NetworkVariable<float> scale = new NetworkVariable<float>(1f);

        private double timestamp;
        private float untilSync;

        public double Timestamp => timestamp;

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
            return DayCycle.IsNight(DayCycle.FractionOf((long)timestamp));
        }

        public string DateTime()
        {
            return DateTimeOffset.FromUnixTimeSeconds(Delta2026 + (long)timestamp).ToString("yyyy.MM.dd HH:mm:ss");
        }

        private void Step()
        {
            float dt = NetworkManager.LocalTime.FixedDeltaTime;
            timestamp += dt * scale.Value * (DayCycle.DayLengthSeconds / DayRealSeconds);

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
