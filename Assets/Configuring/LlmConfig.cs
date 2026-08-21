using Unity.Properties;

namespace Shooter.Configuring
{
    public class LlmConfig
    {
        [CreateProperty]
        public string Key { get; set; }
        [CreateProperty]
        public string Provider { get; set; }

        [CreateProperty]
        public string Model { get; set; }

        public static LlmConfig LlmBase()
        {
            return new LlmConfig
            {
                Key = "",
                Provider = "Polza",
                Model = "google/gemini-3.6-flash"
            };
        }

        public static LlmConfig LlmMax()
        {
            return new LlmConfig
            {
                Key = "",
                Provider = "Polza",
                Model = "anthropic/claude-opus-5"
            };
        }
    }
}
