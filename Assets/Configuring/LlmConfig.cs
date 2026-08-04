namespace Shooter.Configuring
{
    public class LlmConfig
    {
        public string Key { get; set; } = "";
        public string Provider { get; set; } = "Polza";

        public string Model { get; set; } = "anthropic/claude-haiku-4.5";
    }
}
