using System.Linq;
using Unity.Properties;

namespace Shooter.Infrastructure
{
    public class LlmConfig
    {
        private string effort = LlmEfforts.Default;

        [CreateProperty]
        public string Key { get; set; } = "";

        [CreateProperty]
        public string Provider { get; set; } = "Polza";

        [CreateProperty]
        public string Model { get; set; } = "anthropic/claude-haiku-5.5";

        [CreateProperty]
        public string Effort
        {
            get => effort;
            set => effort = LlmEfforts.Keys.Contains(value) ? value : LlmEfforts.Default;
        }
    }
}
