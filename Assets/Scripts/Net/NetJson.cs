using Newtonsoft.Json;
using Shooter.Logging;
using Shooter.Net.Msgs;

namespace Shooter.Net
{
    public static class NetJson
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = { new MsgConverter() },
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public static string Serialize(Msg msg)
        {
            return JsonConvert.SerializeObject(msg, Settings);
        }

        public static Msg Deserialize(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Msg>(json, Settings);
            }
            catch (JsonException e)
            {
                Log.Warn("net: dropped bad message: " + e.Message);
                return null;
            }
        }

        public static T Deserialize<T>(string json) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, Settings);
            }
            catch (JsonException e)
            {
                Log.Warn("net: dropped bad payload: " + e.Message);
                return null;
            }
        }
    }
}
