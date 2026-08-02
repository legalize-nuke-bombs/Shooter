using System.Collections.Generic;
using Shooter.Game.Body;
using Shooter.Game.Llm;
using Unity.Netcode;

namespace Shooter.Game.Speech
{
    public static class TalkPrompt
    {
        private const string TalkRules =
            "Сейчас игрок обращается к тебе. Отвечай на языке игрока.\n" +
            "Твои ответы должны быть краткими, сухими или напряженными. Не пытайся понравиться игроку.\n" +
            "Никогда не используй вежливые клише ИИ (например, 'Чем могу помочь?', 'С радостью отвечу').\n" +
            "Сообщения помечены игровым временем. Учитывай время между репликами.\n" +
            "Если история пуста, это ваш первый контакт.\n" +
            "Если ты чего-то не знаешь, реагируй уклончиво, подозрительно или смени тему, не признавая свою неосведомленность напрямую.";

        public static string Situation(NetworkObject user)
        {
            return new Prompt()
                .Section("Разговор", TalkRules + "\n\nСостояние игрока:\n" + Digestion.Of(user, DigestionDetail.Full))
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
