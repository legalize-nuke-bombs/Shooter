namespace Shooter.Configuring
{
    public class ServerConfig
    {
        public ushort Port { get; set; } = 7777;

        public string World { get; set; } = "New world";

        public string SavesFolder { get; set; } = "Saves";

        public int SavesKept { get; set; } = 10;

        public LlmConfig LlmBase { get; set; } = LlmConfig.LlmBase();
        public LlmConfig LlmMax { get; set; } = LlmConfig.LlmMax();
    }
}
