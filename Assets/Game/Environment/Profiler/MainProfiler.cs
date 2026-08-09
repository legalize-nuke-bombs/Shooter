using System.Text;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game
{
    public class MainProfiler : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private BaseProfiler[] profilers;

        [SerializeField] private float loggingInterval = 10f;
        private float sinceLastLog = 0f;

        private void Awake()
        {
            profilers = GetComponentsInChildren<BaseProfiler>();
            Log.Info($"Found {profilers.Length} profilers");
        }

        private void Update()
        {
            sinceLastLog += Time.deltaTime;
            if (sinceLastLog >= loggingInterval)
            {
                sinceLastLog = 0f;
                WriteLogs();
            }
        }

        private void WriteLogs()
        {
            var sb = new StringBuilder();
            foreach (BaseProfiler profiler in profilers)
            {
                string logLine = profiler?.LogLine();
                if (logLine != null)
                {
                    sb.Append($"{profiler.GetType().Name} {logLine}");
                }
            }

            if (sb.Length > 0)
            {
                Log.Info(sb.ToString());
            }

        }
    }
}
