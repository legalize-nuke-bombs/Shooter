using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shooter.Infrastructure
{
    public static class SaveJson
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            Converters =
            {
                new Vector3JsonConverter(), new QuaternionJsonConverter(), new SaveableJsonConverter(), new SaveTokenJsonConverter(),
                new StringEnumConverter()
            }
        };

        public static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);
    }
}
