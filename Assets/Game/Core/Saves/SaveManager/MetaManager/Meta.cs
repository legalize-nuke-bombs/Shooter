using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Shooter.Game.Core.Saves
{
    public struct Meta
    {
        public static readonly JsonSerializerSettings Json = new()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        };

        public string Version { get; set; }
        public DateTime Stamp { get; set; }
        public DateTime Clock { get; set; }
    }
}
