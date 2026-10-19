using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public sealed class Register
    {
        private readonly List<MonoBehaviour> members = new();

        public IEnumerable<MonoBehaviour> GetMembers(RegisterInactivePolicy inactivePolicy)
        {
            return inactivePolicy == RegisterInactivePolicy.Include ? members : members.Where(member => member.gameObject.activeInHierarchy);
        }

        public void AddMember(MonoBehaviour member)
        {
            members.Add(member);
        }

        public void RemoveMember(MonoBehaviour member)
        {
            members.Remove(member);
        }
    }
}
