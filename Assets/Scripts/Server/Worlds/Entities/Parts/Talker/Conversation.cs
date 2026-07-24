using System.Collections.Generic;
using System.Linq;

namespace Shooter.Server.Worlds.Entities.Parts.Talker
{
    public sealed class Conversation
    {
        private readonly List<Message> messages = new List<Message>();

        public Conversation(Entity user)
        {
            User = user;
        }

        public Entity User { get; private set; }

        public IReadOnlyList<Message> Messages => messages;

        public void Follow(Entity user)
        {
            User = user;
        }

        public void Add(Message message)
        {
            messages.Add(message);
        }

        public Message Last()
        {
            return messages.LastOrDefault();
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
