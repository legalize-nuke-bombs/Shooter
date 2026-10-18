using System.Collections.Generic;
using System.Linq;

namespace Shooter.Infrastructure
{
    public sealed class Register
    {
        private readonly List<RegisteredBehaviour> members = new();

        public IEnumerable<RegisteredBehaviour> All(Inactive gate)
        {
            return gate == Inactive.Include ? members : members.Where(member => member.gameObject.activeInHierarchy);
        }

        public void Add(RegisteredBehaviour member)
        {
            members.Add(member);
        }

        public void Remove(RegisteredBehaviour member)
        {
            members.Remove(member);
        }
    }
}
