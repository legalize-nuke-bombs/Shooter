using Unity.Properties;

namespace Shooter.Configuring
{
    public class ClientConfig
    {
        [CreateProperty]
        public string Name { get; set; } = "Player";

        [CreateProperty]
        public string Invite { get; set; } = "";
    }
}
