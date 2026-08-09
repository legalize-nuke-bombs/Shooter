using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shooter.Logging;
using Unity.Collections;

namespace Shooter.Game.Body.Notifying
{
    public static class NotificationPacking
    {
        private static readonly Journal Log = Logs.Here();

        private const string KindField = "kind";

        private static readonly Dictionary<string, Type> Kinds = Known();

        public static FixedString4096Bytes Pack(Notification notification)
        {
            JObject packed = JObject.FromObject(notification);
            packed[KindField] = notification.GetType().Name;

            string state = packed.ToString(Formatting.None);
            int size = Encoding.UTF8.GetByteCount(state);

            if (size <= FixedString4096Bytes.UTF8MaxLengthInBytes) return new FixedString4096Bytes(state);

            Log.Error($"Notification {notification.GetType().Name} takes {size} bytes, more than the {FixedString4096Bytes.UTF8MaxLengthInBytes} the network format holds");

            return default;
        }

        public static Notification Unpack(FixedString4096Bytes packed)
        {
            if (packed.IsEmpty) return null;

            JObject parsed = JObject.Parse(packed.ToString());
            var kind = parsed.Value<string>(KindField);

            if (kind == null || !Kinds.TryGetValue(kind, out Type known))
            {
                Log.Error($"Notification of unknown kind {kind} arrived");
                return null;
            }

            return (Notification)parsed.ToObject(known);
        }

        private static Dictionary<string, Type> Known()
        {
            var known = new Dictionary<string, Type>();

            foreach (Type type in typeof(Notification).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(Notification).IsAssignableFrom(type)) continue;

                if (known.TryGetValue(type.Name, out Type taken))
                {
                    Log.Error($"Notifications {taken.FullName} and {type.FullName} share the name {type.Name}, the second one will never arrive");
                    continue;
                }

                known.Add(type.Name, type);
            }

            return known;
        }
    }
}
