using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Shooter.Game.Core.Saves
{
    public struct Snapshot
    {
        public string Version { get; set; }
        public DateTime Stamp { get; set; }
        public Dictionary<string, JObject> GameObjects { get; set; }
    }
}
