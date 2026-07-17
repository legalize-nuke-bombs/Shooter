using Newtonsoft.Json;

namespace Shooter.Net.Msgs
{
    // Base of the client<->server wire protocol. The "type" discriminator is derived
    // from MsgTypes, never set by hand: it is emitted on write from the runtime type,
    // and read back by MsgConverter to pick the concrete message.
    public abstract class Msg
    {
        [JsonProperty("type", Order = -2)]
        public string Type => MsgTypes.TagOf(GetType());
    }
}
