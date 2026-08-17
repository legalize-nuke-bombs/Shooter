using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Core
{
    public static class Entity
    {
        private static readonly Journal Log = Logs.Here();

        public static T Find<T>(this Component member) where T : class
        {
            var found = Root(member).GetComponentInChildren<T>();

            if (found == null)
                Log.Warn($"Entity {Root(member).name} has no {typeof(T).Name} wanted by {member.GetType().Name}");

            return found;
        }

        public static T[] FindAll<T>(this Component member) where T : class
        {
            return Root(member).GetComponentsInChildren<T>();
        }

        private static Transform Root(Component member)
        {
            NetworkObject anchor = member.GetComponentInParent<NetworkObject>();
            return anchor == null ? member.transform : anchor.transform;
        }
    }
}
