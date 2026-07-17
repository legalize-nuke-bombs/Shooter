using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Serialization
{
    public static class Json
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

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

        public static string TypeOf(string json)
        {
            try
            {
                return (string)JObject.Parse(json)["type"];
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
