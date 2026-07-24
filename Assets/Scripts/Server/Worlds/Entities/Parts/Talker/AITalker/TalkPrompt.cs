using System.Collections.Generic;
using Shooter.Server.Worlds.Entities.Parts.Llm;
using Shooter.Server.Worlds.Time;

namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker
{
    public static class TalkPrompt
    {
        public static string System(Entity self, Conversation conversation, Clock clock, string character)
        {
            return
                "Ты NPC в 3D мета хорроре с опциональным кооперативным режимом.\n" +
                "В этом контексте ты всегда общаешься с одним и тем же игроком, даже если игра в кооперативном режиме.\n" +
                "Ты никогда не упоминаешь ничего что связанно с программированием.\n" +
                "Ты отвечаешь на том языке, на котором тебе пишет игрок.\n" +
                "Ты хороший собеседник, но отвечаешь кратко.\n" +
                "Ты видишь историю разговора с игроком; если она пуста, он говорит с тобой в первый раз.\n" +
                "Если игрок спрашивает о том, что тебе неизвестно, или просит сделать что-то, для чего у тебя нет инструментов, то ты находишь лучшую отговорку не отвечать или не делать.\n" +
                "Состояние мира на момент последнего сообщения игрока:\n" +
                $"Игровая виртуальная дата и время: {clock.DateTime()}\n" +
                "Твое состояние:\n" +
                self.Digest() + "\n" +
                "Состояние игрока:\n" +
                conversation.User.Digest() + "\n" +
                character;
        }

        public static IReadOnlyList<LlmMessage> Messages(Conversation conversation)
        {
            var messages = new List<LlmMessage>();
            foreach (Message message in conversation.Messages)
            {
                messages.Add(new LlmMessage
                {
                    Role = message.Author == MessageAuthor.Player ? LlmRole.User : LlmRole.Model,
                    Content = message.Content
                });
            }

            return messages;
        }
    }
}
