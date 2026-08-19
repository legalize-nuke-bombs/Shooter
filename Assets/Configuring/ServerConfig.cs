namespace Shooter.Configuring
{
    public class ServerConfig
    {
        public ushort Port { get; set; } = 7777;

        public LlmConfig LlmBase { get; set; } = LlmConfig.LlmBase();
        public LlmConfig LlmMax { get; set; } = LlmConfig.LlmMax();
    }
}
