using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public sealed class Register
    {
        private readonly List<MonoBehaviour> members = new();

        public IEnumerable<MonoBehaviour> All(RegisterInactivePolicy gate)
        {
            return gate == RegisterInactivePolicy.Include ? members : members.Where(member => member.gameObject.activeInHierarchy);
        }

        public void Add(MonoBehaviour member)
        {
            members.Add(member);
        }

        public void Remove(MonoBehaviour member)
        {
            members.Remove(member);
        }
    }
}
