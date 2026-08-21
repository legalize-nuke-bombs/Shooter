using System;

namespace Shooter.Game.Core.Saves
{
    public struct Meta
    {
        public string Version { get; set; }
        public DateTime Stamp { get; set; }
        public DateTime Clock { get; set; }
    }
}
