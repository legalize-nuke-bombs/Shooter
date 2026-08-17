using System.Collections.Generic;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core
{
    public sealed class Register<T> where T : Component
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<long, T> members = new();

        private long counter;

        public int Count => members.Count;

        public IEnumerable<T> All => members.Values;

        public T Of(long id)
        {
            return members.GetValueOrDefault(id, null);
        }

        public long Add(T member)
        {
            long id = counter++;
            members[id] = member;

            return id;
        }

        public void Remove(long id)
        {
            members.Remove(id);
        }
    }
}
