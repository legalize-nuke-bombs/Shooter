using Newtonsoft.Json;
using Shooter.Accounts;
using Unity.Properties;

namespace Shooter.Configuring
{
    public class GameConfig
    {
        public const string FileName = "config.json";

        [JsonConverter(typeof(AccountConverter))]
        public Account Account { get; set; }

        [CreateProperty]
        public ServerConfig Server { get; set; } = new();

        [CreateProperty]
        public ClientConfig Client { get; set; } = new();

        [CreateProperty]
        public LoggingConfig Logging { get; set; } = new();
    }
}
