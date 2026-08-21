using Unity.Properties;

namespace Shooter.Configuring
{
    public class ClientConfig
    {
        [CreateProperty]
        public string Address { get; set; } = "127.0.0.1";

        [CreateProperty]
        public ushort Port { get; set; } = 7777;

        [CreateProperty]
        public string Name { get; set; } = "Player";
    }
}
