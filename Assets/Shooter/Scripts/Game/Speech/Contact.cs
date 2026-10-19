using System;
using System.Collections.Generic;

namespace Shooter.Game
{
    public sealed class Contact
    {
        private readonly Dictionary<int, ConversationLine> lines = new();

        public Contact(long partnerId)
        {
            PartnerId = partnerId;
        }

        public long PartnerId { get; }

        // One past the highest index known so far
        public int LineCount { get; private set; }

        public DateTime LastLineTime { get; private set; }

        public int SeenLineCount { get; set; }

        public int UnreadLineCount => Math.Max(0, LineCount - SeenLineCount);

        public bool TryGetLine(int index, out ConversationLine line)
        {
            return lines.TryGetValue(index, out line);
        }

        public void PutLine(int index, ConversationLine line)
        {
            lines[index] = line;
            if (index >= LineCount) LineCount = index + 1;
            if (line.Time > LastLineTime) LastLineTime = line.Time;
        }
    }
}
