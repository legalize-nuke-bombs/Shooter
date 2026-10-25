using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class Clock : ShooterNetworkBehaviour, ISaveableComponent
    {
        public const string StampFormat = "yyyy.MM.dd HH:mm:ss";
        public const long DayLengthSeconds = 86400;
        public const long HourLengthSeconds = 3600;
        private const float RealSecondsPerGameDay = 2400f;
        private const float GameSecondsPerRealSecond = DayLengthSeconds / RealSecondsPerGameDay;

        [SerializeField] private float latitude = 55.75f;

        [SerializeField] private float declination = -12.33f;

        [SerializeField] private float solarNoonHours = 12.75f;

        [SerializeField] private int beginningYear = 2026;
        [SerializeField] private int beginningMonth = 9;
        [SerializeField] private int beginningDay = 1;
        [SerializeField] private int beginningHour = 22;
        [SerializeField] private int beginningMinutes;
        [SerializeField] private int beginningSeconds;

        private readonly NetworkVariable<double> timestamp = new();
        private readonly NetworkVariable<float> scale = new(1f);

        private int hourPassedCursor;
        private float hourPassedCallsOwed;

        private struct SaveData
        {
            public double Timestamp { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Timestamp = timestamp.Value
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            timestamp.Value = saveData.Timestamp;
        }

        private DateTime Beginning => new(beginningYear, beginningMonth, beginningDay, beginningHour, beginningMinutes, beginningSeconds);

        public double Timestamp => timestamp.Value;

        public DateTime Now => Beginning.AddSeconds(Timestamp);

        public double DayFraction => Now.TimeOfDay.TotalSeconds / DayLengthSeconds;

        public float Latitude => latitude;

        public float Declination => declination;

        public double HourAngle => (DayFraction - solarNoonHours / 24.0) * 360.0;

        public float Scale
        {
            get => scale.Value;
            set
            {
                if (IsServer) scale.Value = value;
            }
        }

        public float DeltaTime => Time.deltaTime * Scale * GameSecondsPerRealSecond;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick += AdvanceTime;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick -= AdvanceTime;
        }

        [ContextMenu("Fast forward x10")]
        private void FastForwardTenTimes()
        {
            Scale = 10f;
        }

        [ContextMenu("Fast forward x60")]
        private void FastForwardSixtyTimes()
        {
            Scale = 60f;
        }

        [ContextMenu("Normal speed")]
        private void SetNormalSpeed()
        {
            Scale = 1f;
        }

        private void AdvanceTime()
        {
            timestamp.Value += NetworkManager.LocalTime.FixedDeltaTime * scale.Value * GameSecondsPerRealSecond;
        }

        private void Update()
        {
            float seconds = DeltaTime;
            TellTimePassed(seconds);
            TellHourPassed(seconds);
        }

        private static void TellTimePassed(float seconds)
        {
            int i = 0;
            ReadOnlySpan<ITimePassedHandler> handlers = Registers.GetAll<ITimePassedHandler>(RegisterInactivePolicy.Exclude);
            while (i < handlers.Length)
            {
                ITimePassedHandler handler = handlers[i];
                handler.HandleTimePassed(seconds);
                handlers = Registers.GetAll<ITimePassedHandler>(RegisterInactivePolicy.Exclude);
                // A handler that switched itself off gave its slot to the last one, which is told next
                if (i < handlers.Length && handlers[i] == handler) i++;
            }
        }

        // Every handler is told once per world hour, the calls spread evenly over the hour instead of all at once
        private void TellHourPassed(float seconds)
        {
            ReadOnlySpan<IHourPassedHandler> handlers = Registers.GetAll<IHourPassedHandler>(RegisterInactivePolicy.Exclude);
            hourPassedCallsOwed = Mathf.Min(hourPassedCallsOwed + handlers.Length * seconds / HourLengthSeconds, handlers.Length);
            while (hourPassedCallsOwed >= 1f && handlers.Length > 0)
            {
                if (hourPassedCursor >= handlers.Length) hourPassedCursor = 0;

                IHourPassedHandler handler = handlers[hourPassedCursor];
                handler.HandleHourPassed();
                hourPassedCallsOwed--;
                handlers = Registers.GetAll<IHourPassedHandler>(RegisterInactivePolicy.Exclude);
                if (hourPassedCursor < handlers.Length && handlers[hourPassedCursor] == handler) hourPassedCursor++;
            }
        }
    }
}
