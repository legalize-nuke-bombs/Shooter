using System;
using System.Collections.Generic;
using Shooter.Game.Body.Notifying;
using Shooter.Logging;

namespace Shooter.Client.Interface.Overlays
{
    public static class NotificationLines
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly Dictionary<Type, NotificationLine> Kinds = Known();

        public static NotificationLine Of(Notification notification)
        {
            return Kinds.GetValueOrDefault(notification.GetType());
        }

        private static Dictionary<Type, NotificationLine> Known()
        {
            var known = new Dictionary<Type, NotificationLine>();

            foreach (Type type in typeof(NotificationLines).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(NotificationLine).IsAssignableFrom(type)) continue;

                var line = (NotificationLine)Activator.CreateInstance(type);

                if (known.TryGetValue(line.Kind, out NotificationLine taken))
                {
                    Log.Error($"Notification lines {taken.GetType().FullName} and {type.FullName} both draw {line.Kind.Name}, the second one stays unused");
                    continue;
                }

                known.Add(line.Kind, line);
            }

            return known;
        }
    }
}
