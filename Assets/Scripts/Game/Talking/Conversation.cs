using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

namespace Shooter.Game.Talking
{
    public sealed class Conversation
    {
        private readonly List<Message> messages = new List<Message>();

        public Conversation(NetworkObject user)
        {
            User = user;
        }

        public NetworkObject User { get; private set; }

        public bool Open { get; private set; }

        public IReadOnlyList<Message> Messages => messages;

        public void Follow(NetworkObject user)
        {
            User = user;
        }

        public void Reopen(NetworkObject user)
        {
            Follow(user);
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
