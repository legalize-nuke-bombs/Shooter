using System;
using System.Collections.Generic;

namespace Shooter.Net.Msgs
{
    public static class MsgTypes
    {
        private static readonly Dictionary<string, Type> ByTag = new Dictionary<string, Type>
        {
            { "hello", typeof(HelloMsg) },
            { "joinWorld", typeof(JoinWorldMsg) },
            { "input", typeof(InputMsg) },
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
