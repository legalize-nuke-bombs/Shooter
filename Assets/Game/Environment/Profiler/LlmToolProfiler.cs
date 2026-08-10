using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shooter.Game.Profiling
{
    public class LlmToolProfiler : BaseProfiler
    {
        private readonly Dictionary<string, long> usage = new Dictionary<string, long>();

        public void RegisterTool(string type)
        {
            usage.TryAdd(type, 0);
            usage[type]++;
        }

        public override string LogLine()
        {
            if (usage == null || usage.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();

            var sortedUsage = usage.OrderByDescending(kvp => kvp.Value);

            bool first = true;
            foreach (var kvp in sortedUsage)
            {
                if (!first) sb.Append(", ");
                sb.Append(kvp.Key).Append(": ").Append(kvp.Value);
                first = false;
            }

            return sb.ToString();
        }
    }
}
