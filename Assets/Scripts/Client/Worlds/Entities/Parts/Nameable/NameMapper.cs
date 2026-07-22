using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using UnityEngine;

namespace Shooter.Client.Worlds.Entities.Parts.Nameable
{
    public static class NameMapper
    {
        public static string NameOf(NameableState nameable)
        {
            if (nameable == null)
            {
                return "";
            }

            switch (nameable.Type)
            {
                case NameableType.SpecialAbsolute:
                    return nameable.Payload;
                case NameableType.SpecialCorrupted:
                    return Corrupted();


                case NameableType.Capsule:
                    return "Капсула";


                default:
                    Log.Warn("Unexpected nameable type {}", nameable.Type);
                    return nameable.Type.ToString();
            }
        }

        private static string Corrupted()
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int length = 8;

            char[] noise = new char[length];

            for (int i = 0; i < length; i++)
                noise[i] = alphabet[Random.Range(0, alphabet.Length)];
            return new string(noise);
        }
    }
}
