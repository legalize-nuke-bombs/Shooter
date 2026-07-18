using UnityEngine;

namespace Shooter.Server.Worlds.Entities.Npcs.Specs.Nameable
{
    public class CorruptedNameable : INameable
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int Length = 8;

        private readonly char[] noise = new char[Length];

        public string Name()
        {
            for (int i = 0; i < Length; i++)
                noise[i] = Alphabet[Random.Range(0, Alphabet.Length)];
            return new string(noise);
        }
    }
}
