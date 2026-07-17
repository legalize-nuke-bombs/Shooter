using Newtonsoft.Json;
using Shooter.Logging;
using Shooter.Net.Msgs;

namespace Shooter.Net
{
    // JSON codec for the network protocol - the control channel and the thin *State
    // projections it carries. Persisting the full domain types to disk will be its own
    // codec later; this one stays client-facing.
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

        // A protocol message, or null if the json is malformed or its "type" is unknown.
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

        // A plain payload outside the Msg union (e.g. the server-to-server hook).
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
