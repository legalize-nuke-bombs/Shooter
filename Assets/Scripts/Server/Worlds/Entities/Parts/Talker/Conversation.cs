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

        public string Prompt()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            return JsonConvert.SerializeObject(messages, settings);
        }

        public ConversationState State()
        {
            var messageStates = new List<MessageState>();
            foreach (Message message in messages)
            {
                messageStates.Add(message.State());
            }

            return new ConversationState
            {
                Messages = messageStates
            };
        }
    }
}
