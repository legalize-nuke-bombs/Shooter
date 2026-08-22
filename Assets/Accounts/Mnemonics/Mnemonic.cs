using System;
using Org.BouncyCastle.Crypto.Digests;

namespace Shooter.Accounts.Mnemonics
{
    public static class Mnemonic
    {
        public static string FromEntropy(byte[] entropy)
        {
            int entropyBits = entropy.Length * 8;
            int checksumBits = entropyBits / 32;
            byte[] hash = Sha256(entropy);
            int words = (entropyBits + checksumBits) / 11;

            var chosen = new string[words];
            for (int w = 0; w < words; w++)
            {
                int index = 0;
                for (int b = 0; b < 11; b++)
                {
                    index = (index << 1) | Bit(entropy, hash, entropyBits, w * 11 + b);
                }

                chosen[w] = Wordlist.Word(index);
            }

            return string.Join(" ", chosen);
        }

        public static byte[] ToEntropy(string phrase)
        {
            string[] tokens = phrase.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            int totalBits = tokens.Length * 11;
            int entropyBits = totalBits * 32 / 33;
            if (entropyBits % 8 != 0)
                throw new FormatException($"A mnemonic of {tokens.Length} words is not a valid length");

            int checksumBits = totalBits - entropyBits;
            var bits = new bool[totalBits];
            for (int w = 0; w < tokens.Length; w++)
            {
                int index = Wordlist.Index(tokens[w].Trim().ToLowerInvariant());
                if (index < 0)
                    throw new FormatException($"The mnemonic word '{tokens[w]}' is not in the list");

                for (int b = 0; b < 11; b++)
                {
                    bits[w * 11 + b] = ((index >> (10 - b)) & 1) == 1;
                }
            }

            var entropy = new byte[entropyBits / 8];
            for (int i = 0; i < entropyBits; i++)
            {
                if (bits[i]) entropy[i / 8] |= (byte)(1 << (7 - i % 8));
            }

            byte[] hash = Sha256(entropy);
            for (int i = 0; i < checksumBits; i++)
            {
                bool expected = ((hash[i / 8] >> (7 - i % 8)) & 1) == 1;
                if (bits[entropyBits + i] != expected)
                    throw new FormatException("The mnemonic checksum does not match, likely a typo");
            }

            return entropy;
        }

        private static int Bit(byte[] entropy, byte[] hash, int entropyBits, int position)
        {
            if (position < entropyBits)
                return (entropy[position / 8] >> (7 - position % 8)) & 1;

            int checksumPosition = position - entropyBits;
            return (hash[checksumPosition / 8] >> (7 - checksumPosition % 8)) & 1;
        }

        private static byte[] Sha256(byte[] data)
        {
            var digest = new Sha256Digest();
            digest.BlockUpdate(data, 0, data.Length);
            var hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);
            return hash;
        }
    }
}
