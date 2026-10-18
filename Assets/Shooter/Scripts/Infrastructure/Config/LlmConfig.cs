using Unity.Properties;

namespace Shooter.Infrastructure
{
    public class LlmConfig
    {
        [CreateProperty]
        public string Key { get; set; } = "";

        [CreateProperty]
        public string Provider { get; set; } = "Polza";

        [CreateProperty]
        public string Model { get; set; } = "google/gemini-3.8-flash";
    }
}
