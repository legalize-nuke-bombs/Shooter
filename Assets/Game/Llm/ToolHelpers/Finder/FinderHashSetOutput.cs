using System.Collections.Generic;

namespace Shooter.Game.Llm.ToolHelpers.Finder
{
    public class FinderHashSetOutput : IFinderOutput
    {
        private readonly HashSet<long> members = new HashSet<long>();

        public void Include(long id)
        {
            members.Add(id);
        }

        public void Exclude(long id)
        {
            members.Remove(id);
        }

        public IEnumerable<long> All()
        {
            return members;
        }
    }
}
