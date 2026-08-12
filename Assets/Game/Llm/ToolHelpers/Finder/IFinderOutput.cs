using System.Collections.Generic;

namespace Shooter.Game.Llm.ToolHelpers.Finder
{
    public interface IFinderOutput
    {
        void Include(long id);
        void Exclude(long id);
        IEnumerable<long> All();
    }
}
