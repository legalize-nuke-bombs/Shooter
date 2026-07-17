using System;
using System.Collections.Generic;

namespace Shooter.Net.Msgs
{
    // Single source of truth for the wire discriminator <-> message type.
    // Register a message here once and it routes in both directions; nothing else
    // touches "type". This is the C# analogue of Jackson's @JsonSubTypes list.
    public static class MsgTypes
    {
        private static readonly Dictionary<string, Type> ByTag = new Dictionary<string, Type>
        {
            // client -> server
            { "hello", typeof(HelloMsg) },
            { "joinWorld", typeof(JoinWorldMsg) },
            { "input", typeof(InputMsg) },
            // server -> client
            { "welcome", typeof(WelcomeMsg) },
            { "worldJoined", typeof(WorldJoinedMsg) },
            { "snapshot", typeof(SnapshotMsg) },
            { "joined", typeof(JoinedMsg) },
            { "left", typeof(LeftMsg) },
        };

        private static readonly Dictionary<Type, string> ByType = Invert(ByTag);

        public static string TagOf(Type type)
        {
            if (ByType.TryGetValue(type, out string tag)) return tag;
            throw new InvalidOperationException("message type not registered in MsgTypes: " + type.Name);
        }

        public static bool TryResolve(string tag, out Type type)
        {
            return ByTag.TryGetValue(tag, out type);
        }

        private static Dictionary<Type, string> Invert(Dictionary<string, Type> byTag)
        {
            var byType = new Dictionary<Type, string>(byTag.Count);
            foreach (KeyValuePair<string, Type> entry in byTag)
                byType[entry.Value] = entry.Key;
            return byType;
        }
    }
}
