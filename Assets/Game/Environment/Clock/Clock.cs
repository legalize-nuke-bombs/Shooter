using System;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Clock : NetworkBehaviour
    {
        public const double DawnFraction = 5.5 / 24.0;
        public const double DuskFraction = 20.0 / 24.0;

        public const long DayLengthSeconds = 86400;
        private const float DayRealSeconds = 1200f;
        private const float SyncInterval = 1f;

        private static readonly DateTimeOffset Beginning = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

        private readonly NetworkVariable<double> synced = new NetworkVariable<double>();
        private readonly NetworkVariable<float> scale = new NetworkVariable<float>(1f);

        private double timestamp;
        private float untilSync;

        public double Timestamp => timestamp;

        public DateTimeOffset Now => Beginning.AddSeconds(timestamp);

        public double DayFraction => Now.TimeOfDay.TotalSeconds / DayLengthSeconds;

        public double SunOverhead
        {
            get
            {
                double fraction = DayFraction;
                if (fraction >= DawnFraction && fraction < DuskFraction)
                    return (fraction - DawnFraction) / (DuskFraction - DawnFraction) * 180.0;

                double sinceDusk = fraction >= DuskFraction ? fraction - DuskFraction : fraction + 1.0 - DuskFraction;
                return 180.0 + sinceDusk / (1.0 - DuskFraction + DawnFraction) * 180.0;
            }
        }

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

        [ContextMenu("Fast forward x10")]
        private void FastForward()
        {
            Scale = 10f;
        }

        [ContextMenu("Fast forward x60")]
        private void FastForwardHard()
        {
            Scale = 60f;
        }

        [ContextMenu("Normal speed")]
        private void NormalSpeed()
        {
            Scale = 1f;
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
