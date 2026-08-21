using System.Collections.Generic;
using System.Linq;
using Shooter.Game.Core.Saves;

namespace Shooter.Game.Speech
{
    public sealed class Conversation : ISaveable
    {
        private readonly List<Message> messages = new();

        private struct SaveData
        {
            public long Wanderer { get; set; }
            public List<Message> Messages { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Wanderer = Wanderer,
                Messages = messages.ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            Wanderer = sd.Wanderer;
            messages.Clear();
            foreach (Message message in sd.Messages) messages.Add(message);
        }

        public Conversation()
        {
        }

        public Conversation(long wanderer)
        {
            Wanderer = wanderer;
        }

        public long Wanderer { get; private set; }

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
