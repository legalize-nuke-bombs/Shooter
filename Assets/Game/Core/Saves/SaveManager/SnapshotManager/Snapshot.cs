using System.Collections.Generic;

namespace Shooter.Game.Core.Saves
{
    public struct Snapshot
    {
        public Dictionary<string, Dictionary<string, SaveToken>> GameObjects { get; set; }
    }
}
