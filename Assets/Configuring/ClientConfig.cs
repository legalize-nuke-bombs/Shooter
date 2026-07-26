namespace Shooter.Configuring
{
    public class ClientConfig
    {
        public const string FileName = "client.json";

        public string Address { get; set; } = "127.0.0.1";

        public ushort Port { get; set; } = 7777;

        public string Name { get; set; } = "Player";
    }
}
