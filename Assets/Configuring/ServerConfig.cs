using Unity.Properties;

namespace Shooter.Configuring
{
    public class ServerConfig
    {
        [CreateProperty]
        public ushort Port { get; set; } = 7777;

        [CreateProperty]
        public string SaveCompressionAlgorithm { get; set; } = "Zip";

        [CreateProperty]
        public LlmConfig Llm { get; set; } = LlmConfig.Default();
    }
}
