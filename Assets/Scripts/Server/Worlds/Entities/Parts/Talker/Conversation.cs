using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public class Conversation
    {
        private readonly List<Message> messages = new List<Message>();

        public void Add(Message message)
        {
            messages.Add(message);
        }

        public Message? Last()
        {
            return messages.LastOrDefault();
        }

        public override string ToString()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            return JsonConvert.SerializeObject(messages);
        }

        public ConversationState State()
        {
            return new ConversationState
            {
                Messages = messages.ToList()
            };
        }
    }
}
