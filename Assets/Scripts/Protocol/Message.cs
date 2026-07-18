using Newtonsoft.Json.Linq;
using Shooter.Serialization;

namespace Shooter.Protocol
{
    public sealed class Message
    {
        public MessageType Type { get; set; }
        public JToken Payload { get; set; }

        public static string Encode(MessageType type, object payload)
        {
            return Json.Serialize(new Message { Type = type, Payload = Json.ToToken(payload) });
        }

        public static Message Decode(string json)
        {
            return Json.Deserialize<Message>(json);
        }

        public T Read<T>()
        {
            return Json.FromToken<T>(Payload);
        }
    }
}
