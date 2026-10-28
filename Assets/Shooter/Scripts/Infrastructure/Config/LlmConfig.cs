using Unity.Properties;

namespace Shooter.Infrastructure
{
    public class LlmConfig
    {
        [CreateProperty]
        public string Key { get; set; } = "";

        [CreateProperty]
        public string Provider { get; set; } = "Claude";

        [CreateProperty]
        public string Model { get; set; } = "claude-haiku-5-5";

        [CreateProperty]
        public string Effort { get; set; } = "";
    }
}
