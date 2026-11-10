using System;
using System.Collections.Generic;

namespace Shooter.Game
{
    public sealed class Contact
    {
        private readonly List<ConversationLine> lines = new();

        public Contact(long partnerId)
        {
            PartnerId = partnerId;
        }

        public long PartnerId { get; }

        public IReadOnlyList<ConversationLine> Lines => lines;

        public DateTime LastLineTime => lines.Count == 0 ? DateTime.MinValue : lines[lines.Count - 1].Time;

        public int SeenLineCount { get; set; }

        public int UnreadLineCount => Math.Max(0, lines.Count - SeenLineCount);

        public void ReplaceLines(IEnumerable<ConversationLine> history)
        {
            lines.Clear();
            lines.AddRange(history);
        }

        public void AddLine(ConversationLine line)
        {
            lines.Add(line);
        }
    }
}
