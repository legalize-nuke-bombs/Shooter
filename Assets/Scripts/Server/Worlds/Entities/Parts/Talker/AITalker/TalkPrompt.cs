using System.Collections.Generic;
using Shooter.Server.Worlds.Entities.Parts.Llm;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker
{
    public static class TalkPrompt
    {
        public static string Situation(Entity user)
        {
            return new Prompt()
                .Section("Состояние игрока, с которым говоришь", user.Digest())
                .ToString();
        }

        public static IReadOnlyList<LlmMessage> Messages(Conversation conversation)
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
