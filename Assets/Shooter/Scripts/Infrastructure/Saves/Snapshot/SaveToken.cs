using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Infrastructure
{
    public readonly struct SaveToken
    {
        private readonly JToken jsonToken;

        private SaveToken(JToken jsonToken)
        {
            this.jsonToken = jsonToken;
        }

        public bool IsEmpty => jsonToken == null || jsonToken.Type == JTokenType.Null;

        public static SaveToken FromObject(object value)
        {
            return new SaveToken(JToken.FromObject(value, SaveJson.Serializer));
        }

        public T ToObject<T>()
        {
            return jsonToken.ToObject<T>(SaveJson.Serializer);
        }

        internal static SaveToken ReadFromJson(JsonReader reader)
        {
            return new SaveToken(JToken.Load(reader));
        }

        internal void WriteToJson(JsonWriter writer)
        {
            jsonToken.WriteTo(writer);
        }
    }
}
