using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shooter.Game.Core.Saves;
using Shooter.Game.World;

namespace Shooter.Game.Speech
{
    public sealed class Conversation : ISaveable
    {
        private readonly List<Message> messages = new();

        private struct SaveData
        {
            public long First { get; set; }
            public long Second { get; set; }
            public List<Message> Messages { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                First = First,
                Second = Second,
                Messages = messages.ToList()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            First = sd.First;
            Second = sd.Second;
            messages.Clear();
            foreach (Message message in sd.Messages) messages.Add(message);
        }

        public Conversation()
        {
        }

        public Conversation(long first, long second)
        {
            (First, Second) = Pair(first, second);
        }

        public long First { get; private set; }

        public long Second { get; private set; }

        public IReadOnlyList<Message> Messages => messages;

        public Message Say(long authorId, string content)
        {
            var message = new Message
            {
                AuthorId = authorId,
                Content = content,
                Time = Clock.Current == null
                    ? string.Empty
                    : Clock.Current.Now.ToString(Message.TimeFormat, CultureInfo.InvariantCulture)
            };

            messages.Add(message);
            return message;
        }

        public static (long, long) Pair(long first, long second)
        {
            return first <= second ? (first, second) : (second, first);
        }
    }
}
