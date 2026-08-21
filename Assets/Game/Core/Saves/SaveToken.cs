using Newtonsoft.Json.Linq;

namespace Shooter.Game.Core.Saves
{
    public readonly struct SaveToken
    {
        private readonly JToken token;

        public SaveToken(JToken token)
        {
            this.token = token;
        }

        public T To<T>()
        {
            return token.ToObject<T>(SaveJson.Serializer);
        }
    }
}
