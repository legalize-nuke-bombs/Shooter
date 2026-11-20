using System;

namespace Shooter.Infrastructure
{
    public sealed class ReadableIdentityVirtualBase32Dict : ReadableIdentityVirtualDict
    {
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        private readonly int wordLength;
        private readonly int bytesPerWord;

        public ReadableIdentityVirtualBase32Dict(int wordLength)
        {
            if (wordLength < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(wordLength));
            }

            this.wordLength = wordLength;
            this.bytesPerWord = (wordLength * 5 + 7) / 8;
        }

        public override void NextWord(ReadOnlySpan<byte> bytes, int begin, out string word, out int next)
        {
            if ((uint)begin >= (uint)bytes.Length)
            {
                word = null;
                next = begin;
                return;
            }

            int available = Math.Min(bytesPerWord, bytes.Length - begin);
            Span<char> chars = wordLength <= 256 ? stackalloc char[wordLength + 1] : new char[wordLength + 1];
            chars[0] = '#';

            int buffer = 0;
            int bitsLeft = 0;
            int byteIndex = 0;

            for (int i = 1; i <= wordLength; i++)
            {
                while (bitsLeft < 5)
                {
                    buffer = (buffer << 8) | (byteIndex < available ? bytes[begin + byteIndex++] : 0);
                    bitsLeft += 8;
                }

                bitsLeft -= 5;
                chars[i] = Alphabet[(buffer >> bitsLeft) & 0x1F];
            }

            word = new string(chars);
            next = begin + available;
        }
    }
}
