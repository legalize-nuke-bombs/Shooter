using System;
using System.Collections.Generic;

namespace Shooter.Net.Msgs
{
    public static class MsgTypes
    {
        private static readonly Dictionary<string, Type> ByName = Discover();
        private static readonly Dictionary<Type, string> ByType = Invert(ByName);

        public static string TagOf(Type type)
        {
            if (ByType.TryGetValue(type, out string tag)) return tag;
            throw new InvalidOperationException("not a registered Msg subtype: " + type.Name);
        }

        public static bool TryResolve(string tag, out Type type)
        {
            type = null;
            return tag != null && ByName.TryGetValue(tag, out type);
        }

        private static Dictionary<string, Type> Discover()
        {
            var byName = new Dictionary<string, Type>();
            foreach (Type type in typeof(Msg).Assembly.GetTypes())
                if (type.IsSubclassOf(typeof(Msg)) && !type.IsAbstract)
                    byName[type.Name] = type;
            return byName;
        }

        private static Dictionary<Type, string> Invert(Dictionary<string, Type> byName)
        {
            var byType = new Dictionary<Type, string>(byName.Count);
            foreach (KeyValuePair<string, Type> entry in byName)
                byType[entry.Value] = entry.Key;
            return byType;
        }
    }
}
