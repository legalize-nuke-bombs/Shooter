using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    public sealed class Register<T> where T : Component
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<long, T> members = new Dictionary<long, T>();

        private long counter;

        public int Count => members.Count;

        public IEnumerable<T> All => members.Values;

        public T Of(long id)
        {
            return members.TryGetValue(id, out T found) ? found : null;
        }

        public long Add(T member)
        {
            long id = counter++;
            members[id] = member;

            return id;
        }

        public void Add(long id, T member)
        {
            if (members.TryGetValue(id, out T taken) && taken != member)
            {
                Log.Error($"Entries {taken.name} and {member.name} share the id {id}, the second one stays unreachable");
                return;
            }

            members[id] = member;
        }

        public void Remove(long id)
        {
            members.Remove(id);
        }
    }
}
