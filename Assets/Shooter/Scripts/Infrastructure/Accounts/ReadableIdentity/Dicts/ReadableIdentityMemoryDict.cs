using System;

namespace Shooter.Infrastructure
{
    public abstract class ReadableIdentityMemoryDict : ReadableIdentityVirtualDict
    {
        public abstract ReadOnlySpan<string> Words { get; }

        public override void NextWord(ReadOnlySpan<byte> bytes, int begin, out string word, out int next)
        {
            ReadOnlySpan<string> words = Words;

            if ((uint)begin >= (uint)bytes.Length || words.IsEmpty)
            {
                word = null;
                next = begin;
                return;
            }

            int byteCount = 1;
            for (uint n = (uint)words.Length - 1; (n >>= 8) != 0; byteCount++) { }

            ReadOnlySpan<byte> slice = bytes.Slice(begin, Math.Min(byteCount, bytes.Length - begin));
            ulong value = 0;
            foreach (byte b in slice)
            {
                value = (value << 8) | b;
            }

            word = words[(int)(value % (ulong)words.Length)].ToUpperInvariant();
            next = begin + slice.Length;
        }
    }
}
