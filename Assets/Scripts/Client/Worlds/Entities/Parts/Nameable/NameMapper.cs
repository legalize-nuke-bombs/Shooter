using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities.Parts.Nameable;

namespace Shooter.Client.Worlds.Entities.Parts.Nameable
{
    public class NameMapper
    {
        private const string GlitchAlphabet = "017XREVID#$@%?!&";
        private const float MetaChance = 0.15f;

        private static readonly string[] MetaMessages = { "I_SEE_YOU", "WAKE_UP", "THE_END_IS_NEAR" };

        private static readonly Dictionary<NameKind, string> KindNames = new Dictionary<NameKind, string>
        {
            { NameKind.Kapsul, "Капсул" },
            { NameKind.DeadPlayer, "Пропавший странник" }
        };

        private readonly HashSet<NameKind> unknownKinds = new HashSet<NameKind>();

        public string NameOf(NameableState nameable)
        {
            switch (nameable)
            {
                case GivenNameState given:
                    return given.Name;
                case CorruptedNameState _:
                    return Corrupted();
                case KindNameState kind:
                    return Named(kind.Kind);
                default:
                    return "";
            }
        }

        private string Named(NameKind kind)
        {
            if (KindNames.TryGetValue(kind, out string name)) return name;

            if (unknownKinds.Add(kind)) Log.Warn("No client name for kind {}", kind);
            return kind.ToString();
        }

        private string Corrupted()
        {
            if (Random.Range(0f, 1f) < MetaChance)
            {
                return MetaMessages[Random.Range(0, MetaMessages.Length)];
            }

            int length = Random.Range(10, 20);

            var builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                char symbol = GlitchAlphabet[Random.Range(0, GlitchAlphabet.Length)];
                builder.Append(symbol);
            }

            return builder.ToString();
        }
    }
}
