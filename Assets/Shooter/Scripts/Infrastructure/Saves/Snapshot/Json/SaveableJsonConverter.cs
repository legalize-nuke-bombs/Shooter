using System;
using Newtonsoft.Json;

namespace Shooter.Infrastructure
{
    public class SaveableJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ISaveableObject).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((ISaveableObject)value).SaveObject());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            ISaveableObject saveable = (ISaveableObject)Activator.CreateInstance(objectType);
            saveable.LoadObject(SaveToken.ReadFromJson(reader));
            return saveable;
        }
    }
}
