using System;
using System.Collections.Generic;

namespace Shooter.Game.Speech
{
    // One partner's side of the mirror: lines by their index in the pair, gaps allowed until they arrive
    public sealed class Contact
    {
        private readonly Dictionary<int, Line> lines = new();

        public Contact(long partnerId)
        {
            PartnerId = partnerId;
        }

        public long PartnerId { get; }

        // One past the highest index known so far
        public int Count { get; private set; }

        public DateTime LastTime { get; private set; }

        // How far the player has read, by index
        public int Seen { get; set; }

        public int Unread => Math.Max(0, Count - Seen);

        public bool TryGet(int index, out Line line)
        {
            return lines.TryGetValue(index, out line);
        }

        public void Put(int index, Line line)
        {
            lines[index] = line;
            if (index >= Count) Count = index + 1;
            if (line.Time > LastTime) LastTime = line.Time;
        }
    }
}
