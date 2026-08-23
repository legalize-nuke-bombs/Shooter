using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shooter.Game.Core.Saves
{
    public readonly struct SaveToken
    {
        private readonly JToken token;

        private SaveToken(JToken token)
        {
            this.token = token;
        }

        public static SaveToken From(object value)
        {
            return new SaveToken(JToken.FromObject(value, SaveJson.Serializer));
        }

        public T To<T>()
        {
            return token.ToObject<T>(SaveJson.Serializer);
        }

        internal static SaveToken Read(JsonReader reader)
        {
            return new SaveToken(JToken.Load(reader));
        }

        internal void Write(JsonWriter writer)
        {
            token.WriteTo(writer);
        }
    }
}
