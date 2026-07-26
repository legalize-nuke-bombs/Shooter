namespace Shooter.Configuring
{
    public class ServerConfig
    {
        public const string FileName = "server.json";

        public ushort Port { get; set; } = 7777;

        public string World { get; set; } = "New world";

        public LlmConfig Llm { get; set; } = new LlmConfig();
    }
}
