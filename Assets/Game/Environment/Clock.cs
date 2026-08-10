using System;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Clock : NetworkBehaviour
    {
        [SerializeField] private float latitude = 55.75f;

        [SerializeField] private float declination = 12.33f;

        [SerializeField] private float solarNoonHours = 12.75f;

        public const long DayLengthSeconds = 86400;
        private const float DayRealSeconds = 1200f;
        private const float SyncInterval = 1f;

        [SerializeField] private int beginningYear = 2026;
        [SerializeField] private int beginningMonth = 9;
        [SerializeField] private int beginningDay = 1;
        [SerializeField] private int beginningHour = 22;
        [SerializeField] private int beginningMinutes = 0;
        [SerializeField] private int beginningSeconds = 0;
        private DateTimeOffset Beginning => new DateTimeOffset(beginningYear, beginningMonth, beginningDay, beginningHour, beginningMinutes, beginningSeconds, TimeSpan.Zero);

        private readonly NetworkVariable<double> synced = new NetworkVariable<double>();
        private readonly NetworkVariable<float> scale = new NetworkVariable<float>(1f);

        private double timestamp;
        private float untilSync;

        public double Timestamp => timestamp;

        public DateTimeOffset Now => Beginning.AddSeconds(timestamp);

        public double DayFraction => Now.TimeOfDay.TotalSeconds / DayLengthSeconds;

        public float Latitude => latitude;

        public float Declination => declination;

        public double HourAngle => (DayFraction - solarNoonHours / 24.0) * 360.0;

        public double DawnFraction => (solarNoonHours - HalfDayHours) / 24.0;

        public double DuskFraction => (solarNoonHours + HalfDayHours) / 24.0;

        private double HalfDayHours => Celestial.HalfDayAngle(declination, latitude) / 15.0;

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
