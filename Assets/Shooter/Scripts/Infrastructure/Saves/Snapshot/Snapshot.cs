using System.Collections.Generic;

namespace Shooter.Infrastructure
{
    public struct Snapshot
    {
        public Dictionary<string, object> GameObjects { get; set; }
    }
}
