using System.Collections.Generic;
using System.Linq;

namespace Shooter.Infrastructure
{
    public sealed class Register
    {
        private readonly List<ShooterBehaviour> members = new();

        public IEnumerable<ShooterBehaviour> All(Inactive gate)
        {
            return gate == Inactive.Include ? members : members.Where(member => member.gameObject.activeInHierarchy);
        }

        public void Add(ShooterBehaviour member)
        {
            members.Add(member);
        }

        public void Remove(ShooterBehaviour member)
        {
            members.Remove(member);
        }
    }
}
