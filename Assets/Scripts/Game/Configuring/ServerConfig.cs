namespace Shooter.Game.Configuring
{
    public class ServerConfig
    {
        public const string FileName = "server.json";

        public ushort Port { get; set; } = 7777;

        public string World { get; set; } = "Полигон";
    }
}
