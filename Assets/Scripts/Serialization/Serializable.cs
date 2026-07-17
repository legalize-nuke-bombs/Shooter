using Newtonsoft.Json;

namespace Shooter.Serialization
{
    public abstract class Serializable
    {
        [JsonProperty("type", Order = -2)]
        public string Type => GetType().Name;
    }
}
