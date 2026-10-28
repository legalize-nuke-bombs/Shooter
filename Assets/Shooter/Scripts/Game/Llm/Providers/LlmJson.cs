using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Shooter.Game
{
    public static class LlmJson
    {
        // Dates stay strings: a raw reply goes back to its provider unchanged, and a parsed date would come back reformatted
        public static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateParseHandling = DateParseHandling.None
        };

        public static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        public static JToken Parse(string json)
        {
            return JsonConvert.DeserializeObject<JToken>(json, Settings);
        }

        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }
    }
}
