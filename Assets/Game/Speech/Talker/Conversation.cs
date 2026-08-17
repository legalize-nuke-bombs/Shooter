using System.Collections.Generic;
using System.Linq;

namespace Shooter.Game.Speech
{
    public sealed class Conversation
    {
        private readonly List<Message> messages = new();

        public Conversation(long wanderer)
        {
            Wanderer = wanderer;
        }

        public long Wanderer { get; }

        public bool Open { get; private set; }

        public IReadOnlyList<Message> Messages => messages;

        public void Reopen()
        {
            Open = true;
        }

        public void Close()
        {
            Open = false;
        }

        public void Add(Message message)
        {
            messages.Add(message);
        }

        public Message Last()
        {
            return messages.LastOrDefault();
        }
    }
}
