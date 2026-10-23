using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    public sealed class Conversation : ISaveableObject
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
            SaveData saveData = content.ToObject<SaveData>();
            First = saveData.First;
            Second = saveData.Second;
            messages.Clear();
            foreach (Message message in saveData.Messages)
            {
                messages.Add(message);
            }
        }

        public Conversation()
        {
        }

        public Conversation(long firstId, long secondId)
        {
            (First, Second) = CreateKey(firstId, secondId);
        }

        public long First { get; private set; }

        public long Second { get; private set; }

        public (long, long) Key => (First, Second);

        public IReadOnlyList<Message> Messages => messages;

        public long GetPartnerId(long participantId)
        {
            return participantId == First ? Second : First;
        }

        public void AddMessage(Message message)
        {
            messages.Add(message);
        }

        // A listener may answer inside the Said event, so the newest line is not always the one being heard
        public int IndexOf(Message message)
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(messages[i], message)) return i;
            }

            return -1;
        }

        public static (long, long) CreateKey(long firstId, long secondId)
        {
            return firstId <= secondId ? (firstId, secondId) : (secondId, firstId);
        }

        public List<int> FindMatchingMessageIndexes(Regex regex)
        {
            var indexes = new List<int>();
            for (int i = 0; i < messages.Count; i++)
            {
                if (regex.IsMatch(messages[i].Content))
                {
                    indexes.Add(i);
                }
            }
            return indexes;
        }
    }
}
