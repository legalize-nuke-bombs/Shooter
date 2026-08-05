namespace Shooter.Configuring
{
    public class LlmConfig
    {
        public string Key { get; set; }
        public string Provider { get; set; }

        public string Model { get; set; }

        public static LlmConfig LlmBase()
        {
            return new LlmConfig()
            {
                Key = "",
                Provider = "Polza",
                Model = "anthropic/claude-haiku-4.5"
            };
        }

        public static LlmConfig LlmMax()
        {
            return new LlmConfig()
            {
                Key = "",
                Provider = "Polza",
                Model = "anthropic/claude-fable-5"
            };
        }
    }
}
