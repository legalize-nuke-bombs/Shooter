using System;
using Newtonsoft.Json;

namespace Shooter.Game.Core.Saves
{
    public class SaveableConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ISaveable).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((ISaveable)value).SaveObject());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var saveable = (ISaveable)Activator.CreateInstance(objectType);
            saveable.LoadObject(SaveToken.Read(reader));
            return saveable;
        }
    }
}
