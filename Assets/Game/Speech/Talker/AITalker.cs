using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shooter.Game.Llm;

namespace Shooter.Game.Speech
{
    public sealed class AITalker : Talker
    {
        private Llm.Llm llm;

        private void Awake()
        {
            llm = GetComponent<Llm.Llm>();
        }

        protected override Task<string> Answer(Conversation conversation)
        {
            if (llm == null)
            {
                throw new InvalidOperationException($"Entity {name} has no llm to answer with");
            }

            return llm.Answer(Messages(conversation));
        }

        private static IReadOnlyList<LlmMessage> Messages(Conversation conversation)
        {
            var messages = new List<LlmMessage>();

            foreach (Message message in conversation.Messages)
            {
                messages.Add(new LlmMessage
                {
                    Role = message.Author == MessageAuthor.Player ? LlmRole.User : LlmRole.Model,
                    Content = message.Content,
                    Time = message.Time
                });
            }

            return messages;
        }
    }
}
