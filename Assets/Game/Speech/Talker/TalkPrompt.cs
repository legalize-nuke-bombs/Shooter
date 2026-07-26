using System.Collections.Generic;
using Unity.Netcode;
using Shooter.Game.Digesting;
using Shooter.Game.Llm;

namespace Shooter.Game.Talking
{
    public static class TalkPrompt
    {
        private const string TalkRules =
            "Сейчас игрок обращается к тебе, и ты должен ему ответить.\n" +
            "Отвечай на том языке, на котором пишет игрок.\n" +
            "Ты хороший собеседник, но отвечаешь кратко.\n" +
            "Сообщения помечены игровым временем: по меткам видно, сколько прошло между репликами.\n" +
            "Если история разговора пуста, игрок говорит с тобой в первый раз.\n" +
            "Если игрок спрашивает о том, чего ты не знаешь, или просит то, чего ты не умеешь, найди лучшую отговорку.";

        public static string Situation(NetworkObject user)
        {
            return new Prompt()
                .Section("Разговор", TalkRules + "\n\nСостояние игрока:\n" + Digestion.Of(user))
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
