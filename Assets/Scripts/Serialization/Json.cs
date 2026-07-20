using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Shooter.Serialization
{
    public static class Json
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = { new StringEnumConverter() }
        };

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }

        public static T Deserialize<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, Settings);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        public static JToken ToToken(object value)
        {
            return JToken.FromObject(value, Serializer);
        }

        public static T FromToken<T>(JToken token)
        {
            return token == null ? default : token.ToObject<T>(Serializer);
        }
    }
}
