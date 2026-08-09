using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Profiling
{
    public class MainProfiler : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<Type, BaseProfiler> profilers = new Dictionary<Type, BaseProfiler>();

        [SerializeField] private float loggingInterval = 10f;
        private float sinceLastLog;

        private bool collected;

        public T Of<T>() where T : BaseProfiler
        {
            Collect();

            if (profilers.TryGetValue(typeof(T), out BaseProfiler profiler))
            {
                return (T)profiler;
            }

            Log.Error($"World has no {typeof(T).Name}, it will count nothing");
            return null;
        }

        private void Collect()
        {
            if (collected) return;

            collected = true;

            foreach (BaseProfiler profiler in GetComponentsInChildren<BaseProfiler>())
            {
                if (!profilers.TryAdd(profiler.GetType(), profiler))
                {
                    Log.Warn($"Profiler {profiler.GetType().Name} is added more than once, only the first one will count");
                }
            }

            Log.Info($"Found {profilers.Count} profilers");
        }

        private void Update()
        {
            sinceLastLog += Time.deltaTime;
            if (sinceLastLog < loggingInterval) return;

            sinceLastLog -= loggingInterval;
            WriteLogs();
        }

        private void WriteLogs()
        {
            Collect();

            var sb = new StringBuilder();
            foreach (BaseProfiler profiler in profilers.Values)
            {
                string logLine = profiler.LogLine();
                if (logLine != null)
                {
                    sb.AppendLine($"{profiler.GetType().Name} {logLine}");
                }
            }

            if (sb.Length > 0)
            {
                Log.Info(sb.ToString());
            }
        }
    }
}
