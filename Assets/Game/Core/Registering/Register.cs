using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game.Core
{
    public sealed class Register
    {
        private readonly List<Component> members = new();

        public IEnumerable<Component> All => members;

        public void Add(Component member)
        {
            members.Add(member);
        }

        public void Remove(Component member)
        {
            members.Remove(member);
        }
    }
}
