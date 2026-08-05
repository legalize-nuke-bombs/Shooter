using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Shooter.Game.Llm
{
    public class LlmConversation
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly List<LlmMessage> messages = new List<LlmMessage>();
        public int PayloadSize { get; private set; } = 0;
        private Action<string> onAnswer = null;

        public bool Pending()
        {
            return messages.Count > 0 && messages[^1].Role == LlmRole.User;
        }

        public string Prompt()
        {
            return JsonConvert.SerializeObject(
                messages
                    .Select(m => new
                        { role = m.Role.Prompt(), content = m.Content, time = m.Time }),
                JsonSettings);
        }

        public void RegisterUserMessage(LlmMessage message, Action<string> onAnswerEvent)
        {
            if (message.Role != LlmRole.User)
            {
                throw new ArgumentException("invalid role");
            }

            messages.Add(message);
            PayloadSize += message.PayloadSize;

            onAnswer = onAnswerEvent;
        }

        public void RegisterModelMessage(LlmMessage message)
        {
            if (message.Role != LlmRole.Model)
            {
                throw new ArgumentException("invalid role");
            }

            messages.Add(message);
            PayloadSize += message.PayloadSize;

            var callback = onAnswer;
            onAnswer = null;

            if (callback != null)
            {
                callback.Invoke(message.Content);
            }
        }

        public void Replace(LlmMessage message)
        {
            if (message.Role != LlmRole.System)
            {
                throw new ArgumentException("invalid role");
            }

            messages.Clear();
            messages.Add(message);
            PayloadSize = message.PayloadSize;
        }
    }
}
