using Newtonsoft.Json;

namespace Shooter.Net.Msgs
{
    public abstract class Msg
    {
        [JsonProperty("type", Order = -2)]
        public string Type => MsgTypes.TagOf(GetType());
    }
}
