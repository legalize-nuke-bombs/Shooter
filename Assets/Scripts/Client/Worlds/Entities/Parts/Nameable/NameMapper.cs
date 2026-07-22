using System.Collections.Generic;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Nameable;
using UnityEngine;

namespace Shooter.Client.Worlds.Entities.Parts.Nameable
{
    public class NameMapper
    {

        private readonly HashSet<NameableType> unknown = new HashSet<NameableType>();

        public string NameOf(NameableState nameable)
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
                    if (unknown.Add(nameable.Type))
                    {
                        Log.Warn("Unexpected nameable type {}", nameable.Type);
                    }
                    return nameable.Type.ToString();
            }
        }

        private string Corrupted()
        {
            const string glitchAlphabet = "017XREVID#$@%?!&";
            string[] metaMessages = { "I_SEE_YOU", "WAKE_UP", "THE_END_IS_NEAR" };

            if (Random.Range(0f, 1f) < 0.15f)
            {
                return metaMessages[Random.Range(0, metaMessages.Length)];
            }

            int length = Random.Range(10, 20);

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < length; i++)
            {
                char c = glitchAlphabet[Random.Range(0, glitchAlphabet.Length)];
                sb.Append(c);
            }

            return sb.ToString();
        }

    }
}
