namespace Shooter.Configuring
{
    public class GameConfig
    {
        public const string FileName = "config.json";

        public ServerConfig Server { get; set; } = new();

        public ClientConfig Client { get; set; } = new();

        public LoggingConfig Logging { get; set; } = new();
    }
}
