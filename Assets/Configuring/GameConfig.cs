using Unity.Properties;

namespace Shooter.Configuring
{
    public class GameConfig
    {
        public const string FileName = "config.json";

        [CreateProperty]
        public string Key { get; set; } = "";

        [CreateProperty]
        public ServerConfig Server { get; set; } = new();

        [CreateProperty]
        public ClientConfig Client { get; set; } = new();

        [CreateProperty]
        public LoggingConfig Logging { get; set; } = new();
    }
}
