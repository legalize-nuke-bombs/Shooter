using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shooter.Game.Core
{
    public sealed class Register
    {
        private readonly List<Component> members = new();

        public IEnumerable<Component> All(Inactive gate)
        {
            return gate == Inactive.Include
                ? members
                : members.Where(member => member.gameObject.activeInHierarchy);
        }

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
