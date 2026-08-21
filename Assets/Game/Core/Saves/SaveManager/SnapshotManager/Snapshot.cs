using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Shooter.Game.Core.Saves
{
    public struct Snapshot
    {
        public Dictionary<string, JObject> GameObjects { get; set; }
    }
}
