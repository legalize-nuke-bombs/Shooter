using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Net.Msgs
{
    // Read side of the discriminated union: reads "type", resolves the concrete message
    // via MsgTypes and deserializes into it. Writing is left to the default serializer,
    // which emits "type" from Msg.Type - so there is no write path here, and resolving
    // into a concrete type (for which CanConvert is false) cannot recurse.
    public class MsgConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Msg);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            JObject o = JObject.Load(reader);
            string tag = (string)o["type"];
            if (tag == null || !MsgTypes.TryResolve(tag, out Type concrete))
                throw new JsonSerializationException("unknown message type '" + tag + "'");

            return o.ToObject(concrete, serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
