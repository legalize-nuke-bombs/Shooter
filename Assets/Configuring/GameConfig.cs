namespace Shooter.Configuring
{
    public class GameConfig
    {
        public const string FileName = "config.json";

        public ServerConfig Server { get; set; } = new ServerConfig();

        public ClientConfig Client { get; set; } = new ClientConfig();

        public LoggingConfig Logging { get; set; } = new LoggingConfig();
    }
}
